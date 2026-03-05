using System;
using System.Collections.Generic;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot.Data
{
    /// <summary>
    /// Matches current lap distance to a reference lap using O(log n) binary search
    /// and linear interpolation between adjacent data points.
    /// </summary>
    public class DistanceMatchEngine
    {
        private double[] _distances = Array.Empty<double>();
        private TelemetrySnapshot[] _snapshots = Array.Empty<TelemetrySnapshot>();
        private bool _hasData;

        public bool HasReference => _hasData;
        public double TrackLength => _hasData && _distances.Length > 0 ? _distances[^1] : 0;

        /// <summary>Load a sorted reference dataset. Must be called before any queries.</summary>
        public void LoadReference(List<TelemetrySnapshot> referenceData)
        {
            if (referenceData == null || referenceData.Count == 0)
            {
                _hasData = false;
                return;
            }

            // Sort by LapDistance so binary search works correctly
            var sorted = new List<TelemetrySnapshot>(referenceData);
            sorted.Sort((a, b) => a.LapDistance.CompareTo(b.LapDistance));

            _distances = new double[sorted.Count];
            _snapshots = new TelemetrySnapshot[sorted.Count];

            for (int i = 0; i < sorted.Count; i++)
            {
                _distances[i] = sorted[i].LapDistance;
                _snapshots[i] = sorted[i];
            }

            _hasData = true;
        }

        /// <summary>
        /// Find the nearest reference point using binary search. O(log n).
        /// </summary>
        public TelemetrySnapshot FindNearest(double lapDistance)
        {
            if (!_hasData) return TelemetrySnapshot.Empty;
            var idx = BinarySearch(lapDistance);
            return _snapshots[idx];
        }

        /// <summary>
        /// Interpolate between the two adjacent reference points.
        /// Formula: V = V1 + (V2 - V1) * (Pos - Pos1) / (Pos2 - Pos1)
        /// </summary>
        public TelemetrySnapshot Interpolate(double lapDistance)
        {
            if (!_hasData) return TelemetrySnapshot.Empty;

            var idx = BinarySearch(lapDistance);

            // Edge cases
            if (idx <= 0) return _snapshots[0];
            if (idx >= _snapshots.Length - 1) return _snapshots[^1];

            var s1 = _snapshots[idx];
            var s2 = _snapshots[idx + 1];
            var d1 = _distances[idx];
            var d2 = _distances[idx + 1];

            if (Math.Abs(d2 - d1) < 0.001) return s1;

            double t = (lapDistance - d1) / (d2 - d1);
            t = Math.Clamp(t, 0.0, 1.0);

            return new TelemetrySnapshot
            {
                LapDistance = lapDistance,
                Throttle = Lerp(s1.Throttle, s2.Throttle, (float)t),
                Brake = Lerp(s1.Brake, s2.Brake, (float)t),
                Steering = Lerp(s1.Steering, s2.Steering, (float)t),
                Gear = t < 0.5 ? s1.Gear : s2.Gear,  // discrete - use nearest
                RPM = Lerp(s1.RPM, s2.RPM, (float)t),
                Speed = Lerp(s1.Speed, s2.Speed, (float)t),
                LapTime = s1.LapTime + (s2.LapTime - s1.LapTime) * t,
                LapNumber = s1.LapNumber,
                TrackName = s1.TrackName,
                VehicleName = s1.VehicleName
            };
        }

        /// <summary>
        /// Returns the interpolated reference snapshot at current position + offsetMeters ahead.
        /// Used for look-ahead predictions (gear changes, brake points).
        /// </summary>
        public TelemetrySnapshot? LookAhead(double lapDistance, double offsetMeters)
        {
            if (!_hasData) return null;
            var targetDist = lapDistance + offsetMeters;
            if (targetDist > TrackLength) targetDist -= TrackLength; // wrap around
            return Interpolate(targetDist);
        }

        // Returns the index of the largest _distances[i] <= lapDistance (lower bound).
        private int BinarySearch(double lapDistance)
        {
            int lo = 0, hi = _distances.Length - 1;

            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_distances[mid] <= lapDistance)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            return lo;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
