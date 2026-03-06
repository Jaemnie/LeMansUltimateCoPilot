using System;
using System.Collections.Generic;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Detects lap start/completion from a TelemetrySnapshot stream.
    /// Uses lap number change and start-line crossing for detection.
    /// </summary>
    public class LapDetector
    {
        private readonly List<TelemetrySnapshot> _currentLapData = new();
        private int _prevLapNumber = -1;
        private double _prevLapDistance = -1;
        private double _lapStartLapTime = 0;
        private bool _lapInProgress = false;

        public event EventHandler<LapCompletedEventArgs>? LapCompleted;

        public void Process(TelemetrySnapshot snap)
        {
            bool newLap = false;

            if (_prevLapNumber < 0)
            {
                // First reading
                _prevLapNumber = snap.LapNumber;
                _prevLapDistance = snap.LapDistance;
                StartLap(snap);
                return;
            }

            // Lap number increased OR start/finish line crossed (distance reset)
            if (snap.LapNumber > _prevLapNumber ||
                (snap.LapDistance < 50 && _prevLapDistance > 200))
            {
                newLap = true;
            }

            if (newLap && _lapInProgress && _currentLapData.Count >= 50)
            {
                FinishLap(snap);
            }

            if (newLap)
            {
                StartLap(snap);
            }
            else if (_lapInProgress)
            {
                _currentLapData.Add(snap);
            }

            _prevLapNumber = snap.LapNumber;
            _prevLapDistance = snap.LapDistance;
        }

        private void StartLap(TelemetrySnapshot snap)
        {
            _currentLapData.Clear();
            _currentLapData.Add(snap);
            _lapInProgress = true;
            _lapStartLapTime = snap.LapTime;
        }

        private void FinishLap(TelemetrySnapshot snap)
        {
            var lapTime = snap.LapTime > 0 ? snap.LapTime : (_currentLapData.Count > 0 ? _currentLapData[^1].LapTime : 0);
            if (lapTime < 30 || lapTime > 600) return; // sanity check

            LapCompleted?.Invoke(this, new LapCompletedEventArgs(
                snap.LapNumber > 0 ? snap.LapNumber - 1 : _prevLapNumber,
                lapTime,
                new List<TelemetrySnapshot>(_currentLapData),
                snap.TrackName,
                snap.VehicleName));

            _lapInProgress = false;
        }

    }

    public class LapCompletedEventArgs : EventArgs
    {
        public int LapNumber { get; }
        public double LapTime { get; }
        public List<TelemetrySnapshot> TelemetryData { get; }
        public string TrackName { get; }
        public string VehicleName { get; }

        public LapCompletedEventArgs(int lapNumber, double lapTime,
            List<TelemetrySnapshot> data, string trackName, string vehicleName)
        {
            LapNumber = lapNumber;
            LapTime = lapTime;
            TelemetryData = data;
            TrackName = trackName;
            VehicleName = vehicleName;
        }
    }

}
