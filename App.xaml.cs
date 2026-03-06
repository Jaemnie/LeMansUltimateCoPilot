using System.Windows;
using LeMansUltimateCoPilot.Data;
using LeMansUltimateCoPilot.Overlay;
using LeMansUltimateCoPilot.Services;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot
{
    public partial class App : Application
    {
        private SharedMemoryReader? _reader;
        private LapDetector? _lapDetector;
        private ReferenceLapStore? _store;
        private DistanceMatchEngine? _matchEngine;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _store = new ReferenceLapStore();
            _matchEngine = new DistanceMatchEngine();
            _reader = new SharedMemoryReader();
            _lapDetector = new LapDetector();

            // Auto-load best reference when track/vehicle detected
            _reader.OnTrackVehicleChanged += (track, vehicle) =>
            {
                var best = _store.LoadBest(track, vehicle);
                if (best != null)
                    _matchEngine.LoadReference(best.ToSnapshots());
            };

            // Auto-save best lap on lap completion
            _lapDetector.LapCompleted += (_, args) =>
            {
                _store.Save(args.TrackName, args.VehicleName, args.LapTime, args.TelemetryData);

                // Reload reference if this was a new best
                var best = _store.LoadBest(args.TrackName, args.VehicleName);
                if (best != null)
                    _matchEngine.LoadReference(best.ToSnapshots());
            };

            _reader.Start();

            var overlay = new OverlayWindow(_reader, _matchEngine, _lapDetector);
            overlay.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _reader?.Dispose();
            base.OnExit(e);
        }
    }
}
