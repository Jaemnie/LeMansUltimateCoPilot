using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot.Data
{
    /// <summary>
    /// Stores and retrieves reference laps as JSON.
    /// Files are saved to %APPDATA%/ApexFollower/ReferenceLaps/
    /// Filename: {Track}_{Vehicle}_{LapTime}s.json
    /// </summary>
    public class ReferenceLapStore
    {
        private static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ApexFollower", "ReferenceLaps");

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

            var file = BuildPath(trackName, vehicleName, lapTime);
            var payload = new ReferenceLapData
            {
                TrackName = trackName,
                VehicleName = vehicleName,
                LapTime = lapTime,
                RecordedAt = DateTime.Now,
                Data = data
            };

            var options = new JsonSerializerOptions { WriteIndented = false };
            File.WriteAllText(file, JsonSerializer.Serialize(payload, options));
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
                catch { /* skip corrupt files */ }
            }

            return best;
        }

        /// <summary>List available reference laps for a track.</summary>
        public List<ReferenceLapInfo> ListAvailable(string trackName)
        {
            var result = new List<ReferenceLapInfo>();
            var pattern = $"{Sanitize(trackName)}_*.json";

            foreach (var f in Directory.GetFiles(BaseDir, pattern))
            {
                try
                {
                    var lap = Load(f);
                    if (lap != null)
                        result.Add(new ReferenceLapInfo(lap.TrackName, lap.VehicleName, lap.LapTime, f));
                }
                catch { }
            }

            return result.OrderBy(x => x.LapTime).ToList();
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
            return JsonSerializer.Deserialize<ReferenceLapData>(json);
        }

        private static string BuildPath(string track, string vehicle, double lapTime)
        {
            var name = $"{Sanitize(track)}_{Sanitize(vehicle)}_{lapTime:F3}s.json";
            return Path.Combine(BaseDir, name);
        }

        private static string Sanitize(string s) =>
            string.Join("_", s.Split(Path.GetInvalidFileNameChars())).Trim('_');
    }

    public class ReferenceLapData
    {
        public string TrackName { get; set; } = "";
        public string VehicleName { get; set; } = "";
        public double LapTime { get; set; }
        public DateTime RecordedAt { get; set; }
        public List<TelemetrySnapshot> Data { get; set; } = new();
    }

    public record ReferenceLapInfo(string TrackName, string VehicleName, double LapTime, string FilePath);
}
