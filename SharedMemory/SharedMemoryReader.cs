using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LeMansUltimateCoPilot.SharedMemory
{
    /// <summary>
    /// Reads LMU/rFactor2 telemetry from shared memory at 100Hz.
    /// External read-only access - safe for anti-cheat.
    /// </summary>
    public class SharedMemoryReader : IDisposable
    {
        private const string TelemetryMapName = "$rFactor2SMMP_Telemetry$";
        private const string ScoringMapName = "$rFactor2SMMP_Scoring$";
        private const int PollIntervalMs = 10; // 100Hz

        private Thread? _thread;
        private volatile bool _running;
        private volatile bool _isConnected;

        // Lock-free snapshot: written by background thread, read by UI thread
        private TelemetrySnapshot _latest = TelemetrySnapshot.Empty;
        private readonly object _snapshotLock = new();

        private string _lastTrackName = "";
        private string _lastVehicleName = "";

        public TelemetrySnapshot Latest
        {
            get { lock (_snapshotLock) { return _latest; } }
            private set { lock (_snapshotLock) { _latest = value; } }
        }

        public bool IsConnected => _isConnected;

        /// <summary>Fired when the track or vehicle changes - triggers auto-load of reference lap.</summary>
        public event Action<string, string>? OnTrackVehicleChanged;

        public void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(ReadLoop) { IsBackground = true, Name = "SharedMemoryReader" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            _thread?.Join(500);
        }

        private void ReadLoop()
        {
            MemoryMappedFile? telMmf = null;
            MemoryMappedFile? scorMmf = null;

            while (_running)
            {
                try
                {
                    if (telMmf == null)
                        telMmf = TryOpen(TelemetryMapName);

                    if (scorMmf == null)
                        scorMmf = TryOpen(ScoringMapName);

                    if (telMmf == null || scorMmf == null)
                    {
                        _isConnected = false;
                        Thread.Sleep(1000);
                        continue;
                    }

                    _isConnected = true;

                    using var telAccessor = telMmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                    using var scorAccessor = scorMmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                    while (_running)
                    {
                        var snapshot = ReadSnapshot(telAccessor, scorAccessor);
                        if (snapshot.HasValue)
                        {
                            var s = snapshot.Value;
                            Latest = s;

                            // Detect track/vehicle change for auto-loading reference
                            if (s.TrackName != _lastTrackName || s.VehicleName != _lastVehicleName)
                            {
                                _lastTrackName = s.TrackName;
                                _lastVehicleName = s.VehicleName;
                                if (!string.IsNullOrEmpty(s.TrackName))
                                    OnTrackVehicleChanged?.Invoke(s.TrackName, s.VehicleName);
                            }
                        }

                        Thread.Sleep(PollIntervalMs);
                    }
                }
                catch (Exception)
                {
                    _isConnected = false;
                    telMmf?.Dispose();
                    scorMmf?.Dispose();
                    telMmf = null;
                    scorMmf = null;
                    Thread.Sleep(2000);
                }
            }

            telMmf?.Dispose();
            scorMmf?.Dispose();
        }

        private static MemoryMappedFile? TryOpen(string name)
        {
            try { return MemoryMappedFile.OpenExisting(name); }
            catch { return null; }
        }

        private static TelemetrySnapshot? ReadSnapshot(
            MemoryMappedViewAccessor tel,
            MemoryMappedViewAccessor scor)
        {
            try
            {
                // Verify telemetry buffer is stable (begin == end)
                var telVerBegin = tel.ReadUInt32(0);
                var telVerEnd = tel.ReadUInt32(4);
                if (telVerBegin != telVerEnd) return null;

                var numVehicles = tel.ReadInt32(12);
                if (numVehicles <= 0) return null;

                // Find player vehicle index in telemetry buffer
                var playerIndex = FindPlayerIndex(tel, scor, numVehicles);
                var vehicleSize = Marshal.SizeOf<rF2VehicleTelemetry>();
                var vehicleOffset = 16 + (playerIndex * vehicleSize);

                // Read vehicle telemetry via marshalling
                var buf = new byte[vehicleSize];
                tel.ReadArray(vehicleOffset, buf, 0, vehicleSize);
                var vt = BytesToStruct<rF2VehicleTelemetry>(buf);

                // Read lap distance from scoring (more accurate than computing from speed)
                var lapDist = ReadPlayerLapDist(scor, vt.mID);

                // Compute speed from local velocity vector (m/s -> km/h)
                var vx = vt.mLocalVel.x;
                var vy = vt.mLocalVel.y;
                var vz = vt.mLocalVel.z;
                var speedMps = Math.Sqrt(vx * vx + vy * vy + vz * vz);

                // Elapsed lap time = session time - lap start time
                var lapTime = vt.mElapsedTime - vt.mLapStartET;

                return new TelemetrySnapshot
                {
                    LapDistance = lapDist >= 0 ? lapDist : 0,
                    Throttle = (float)vt.mUnfilteredThrottle,
                    Brake = (float)vt.mUnfilteredBrake,
                    Steering = (float)vt.mUnfilteredSteering,
                    Gear = vt.mGear,
                    RPM = (float)vt.mEngineRPM,
                    Speed = (float)(speedMps * 3.6),
                    LapTime = lapTime > 0 ? lapTime : 0,
                    LapNumber = vt.mLapNumber,
                    TrackName = DecodeString(vt.mTrackName),
                    VehicleName = DecodeString(vt.mVehicleName)
                };
            }
            catch
            {
                return null;
            }
        }

        private static int FindPlayerIndex(
            MemoryMappedViewAccessor tel,
            MemoryMappedViewAccessor scor,
            int numVehicles)
        {
            try
            {
                // Try to find player's vehicle ID from scoring data
                var scorVerBegin = scor.ReadUInt32(0);
                var scorVerEnd = scor.ReadUInt32(4);
                if (scorVerBegin != scorVerEnd) return 0;

                var scoringSize = Marshal.SizeOf<rF2Scoring>();
                var scorBuf = new byte[scoringSize];
                scor.ReadArray(8, scorBuf, 0, scoringSize);
                var scoring = BytesToStruct<rF2Scoring>(scorBuf);

                int playerVehicleId = -1;
                var sv = scoring.mVehicles;
                int count = Math.Min(scoring.mScoringInfo.mNumVehicles, sv?.Length ?? 0);

                for (int i = 0; i < count; i++)
                {
                    var v = sv![i];
                    if (v.mIsPlayer == 1 || v.mControl == 0)
                    {
                        playerVehicleId = v.mID;
                        break;
                    }
                }

                if (playerVehicleId == -1) return 0;

                // Match player vehicle ID in telemetry array
                var vehicleSize = Marshal.SizeOf<rF2VehicleTelemetry>();
                for (int i = 0; i < numVehicles && i < 128; i++)
                {
                    var id = tel.ReadInt32(16 + i * vehicleSize);
                    if (id == playerVehicleId) return i;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static double ReadPlayerLapDist(MemoryMappedViewAccessor scor, int vehicleId)
        {
            try
            {
                var scorVerBegin = scor.ReadUInt32(0);
                var scorVerEnd = scor.ReadUInt32(4);
                if (scorVerBegin != scorVerEnd) return -1;

                var scoringSize = Marshal.SizeOf<rF2Scoring>();
                var buf = new byte[scoringSize];
                scor.ReadArray(8, buf, 0, scoringSize);
                var scoring = BytesToStruct<rF2Scoring>(buf);

                var sv = scoring.mVehicles;
                int count = Math.Min(scoring.mScoringInfo.mNumVehicles, sv?.Length ?? 0);
                for (int i = 0; i < count; i++)
                {
                    var v = sv![i];
                    if (v.mID == vehicleId || v.mIsPlayer == 1 || v.mControl == 0)
                        return v.mLapDist;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject()); }
            finally { handle.Free(); }
        }

        private static string DecodeString(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        public void Dispose() => Stop();
    }
}
