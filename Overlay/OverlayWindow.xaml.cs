using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using LeMansUltimateCoPilot.Data;
using LeMansUltimateCoPilot.Services;
using LeMansUltimateCoPilot.SharedMemory;

namespace LeMansUltimateCoPilot.Overlay
{
    public partial class OverlayWindow : Window
    {
        // Win32 constants for click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private readonly SharedMemoryReader _reader;
        private readonly DistanceMatchEngine _matchEngine;
        private readonly LapDetector _lapDetector;

        private int _prevLapNumber = -1;

        public OverlayWindow(SharedMemoryReader reader, DistanceMatchEngine matchEngine, LapDetector lapDetector)
        {
            InitializeComponent();
            _reader = reader;
            _matchEngine = matchEngine;
            _lapDetector = lapDetector;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            MakeClickThrough();

            // Hook rendering loop to monitor refresh rate
            CompositionTarget.Rendering += OnRender;
        }

        private void MakeClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
        }

        private void OnRender(object? sender, EventArgs e)
        {
            var snap = _reader.Latest;

            // Update connection status
            if (_reader.IsConnected)
            {
                if (StatusBar.Visibility != Visibility.Collapsed)
                    StatusBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (StatusBar.Visibility != Visibility.Visible)
                    StatusBar.Visibility = Visibility.Visible;
                StatusText.Text = "Waiting for LMU...";
                return;
            }

            // Feed telemetry to lap detector
            if (snap.LapNumber != _prevLapNumber || snap.LapDistance > 0)
            {
                _lapDetector.Process(snap);
                _prevLapNumber = snap.LapNumber;
            }

            // Get reference at current lap distance
            var reference = _matchEngine.HasReference
                ? _matchEngine.Interpolate(snap.LapDistance)
                : TelemetrySnapshot.Empty;

            // Update all HUD components
            BrakeBar.Update(snap.Brake, reference.Brake, isPedalBrake: true);
            ThrottleBar.Update(snap.Throttle, reference.Throttle, isPedalBrake: false);
            GearControl.Update(snap.Gear, reference.Gear, _matchEngine);
            SteeringControl.Update(snap.Steering, reference.Steering);
            DeltaControl.Update(snap.LapTime, reference.LapTime, _matchEngine.HasReference);
        }

        protected override void OnClosed(EventArgs e)
        {
            CompositionTarget.Rendering -= OnRender;
            base.OnClosed(e);
        }
    }
}
