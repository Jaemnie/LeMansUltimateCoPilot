using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot.Data
{
    /// <summary>
    /// Stores and retrieves reference laps as JSON.
    /// Files are saved to %APPDATA%/ApexFollower/ReferenceLaps/
    /// Filename: {Track}_{Vehicle}_{M}m{SS}.{mmm}s.json
    /// </summary>
    public class ReferenceLapStore
    {
        private static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ApexFollower", "ReferenceLaps");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ReferenceLapStore()
        {
            Directory.CreateDirectory(BaseDir);
        }

        /// <summary>Save a lap. Replaces existing if it has a worse time.</summary>
        public bool Save(string trackName, string vehicleName, double lapTime,
                         List<TelemetrySnapshot> data)
        {
            if (string.IsNullOrEmpty(trackName) || data.Count < 50 || lapTime < 30) return false;

            var existing = LoadBest(trackName, vehicleName);
            if (existing != null && existing.LapTime <= lapTime) return false; // not a new best

            var payload = new ReferenceLapData
            {
                Track = trackName,
                Vehicle = vehicleName,
                LapTime = lapTime,
                LapTimeFormatted = FormatLapTime(lapTime),
                RecordedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                DataPointCount = data.Count,
                Data = data.ConvertAll(s => new LapDataPoint(
                    s.LapDistance, s.Throttle, s.Brake, s.Steering,
                    s.Gear, s.RPM, s.Speed, s.LapTime))
            };

            var file = BuildPath(trackName, vehicleName, lapTime);
            File.WriteAllText(file, JsonSerializer.Serialize(payload, JsonOptions));
            return true;
        }

        /// <summary>Load the best (fastest) lap for a given track and vehicle.</summary>
        public ReferenceLapData? LoadBest(string trackName, string vehicleName)
        {
            var files = FindFiles(trackName, vehicleName);
            ReferenceLapData? best = null;

            foreach (var f in files)
            {
                try
                {
                    var lap = Load(f);
                    if (lap != null && (best == null || lap.LapTime < best.LapTime))
                        best = lap;
                }
                catch { /* skip corrupt or legacy-format files */ }
            }

            return best;
        }

        private string[] FindFiles(string trackName, string vehicleName)
        {
            if (!Directory.Exists(BaseDir)) return Array.Empty<string>();
            var prefix = $"{Sanitize(trackName)}_{Sanitize(vehicleName)}_";
            return Directory.GetFiles(BaseDir, $"{prefix}*.json");
        }

        private static ReferenceLapData? Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ReferenceLapData>(json, JsonOptions);
        }

        private static string BuildPath(string track, string vehicle, double lapTime)
        {
            var name = $"{Sanitize(track)}_{Sanitize(vehicle)}_{FormatLapTime(lapTime)}.json";
            // Replace ':' with 'm' so it's valid in Windows filenames  e.g. "1m32.345s"
            name = name.Replace(':', 'm');
            return Path.Combine(BaseDir, name);
        }

        private static string FormatLapTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private static string Sanitize(string s) =>
            string.Join("_", s.Split(Path.GetInvalidFileNameChars())).Trim('_');
    }

    /// <summary>
    /// Lean data point stored in the JSON file.
    /// Omits TrackName/VehicleName/LapNumber which are redundant in saved reference laps.
    /// </summary>
    public record LapDataPoint(
        double Dist,
        float Throttle,
        float Brake,
        float Steering,
        int Gear,
        float Rpm,
        float Speed,
        double LapTime);

    /// <summary>
    /// Reference lap file structure (v1).
    /// </summary>
    public class ReferenceLapData
    {
        /// <summary>Format version - increment when schema changes.</summary>
        public int Version { get; set; } = 1;
        public string Track { get; set; } = "";
        public string Vehicle { get; set; } = "";
        /// <summary>Total lap time in seconds (for computation).</summary>
        public double LapTime { get; set; }
        /// <summary>Human-readable lap time e.g. "1:32.345".</summary>
        public string LapTimeFormatted { get; set; } = "";
        /// <summary>UTC timestamp in ISO 8601 format.</summary>
        public string RecordedAt { get; set; } = "";
        /// <summary>Number of telemetry points (summary, no need to load Data to check).</summary>
        public int DataPointCount { get; set; }
        public List<LapDataPoint> Data { get; set; } = new();

        /// <summary>Convert stored data points back to TelemetrySnapshot list for the match engine.</summary>
        public List<TelemetrySnapshot> ToSnapshots() =>
            Data.ConvertAll(p => new TelemetrySnapshot
            {
                LapDistance = p.Dist,
                Throttle    = p.Throttle,
                Brake       = p.Brake,
                Steering    = p.Steering,
                Gear        = p.Gear,
                RPM         = p.Rpm,
                Speed       = p.Speed,
                LapTime     = p.LapTime,
                LapNumber   = 0,
                TrackName   = Track,
                VehicleName = Vehicle
            });
    }
}
