using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LeMansUltimateCoPilot.Data;

namespace LeMansUltimateCoPilot.Overlay
{
    /// <summary>
    /// Shows current gear and alerts when the reference gear differs.
    /// Color:  Green = match, Blue = upshift needed, Red = downshift needed.
    /// Animation: ScaleTransform pulse when shift alert fires.
    /// </summary>
    public partial class TargetGear : UserControl
    {
        private static readonly Color ColorGreen = Color.FromRgb(0x00, 0xE6, 0x76);
        private static readonly Color ColorBlue  = Color.FromRgb(0x40, 0xC4, 0xFF);
        private static readonly Color ColorRed   = Color.FromRgb(0xFF, 0x52, 0x52);

        private readonly Storyboard _pulse;
        private readonly Storyboard _alertFade;

        private int _lastAlertGear = -99;
        private DateTime _lastAlertTime = DateTime.MinValue;
        private const double AlertCooldownSec = 2.0;

        public TargetGear()
        {
            InitializeComponent();
            _pulse = (Storyboard)Resources["PulseAnimation"];
            _alertFade = (Storyboard)Resources["AlertFadeOut"];
        }

        /// <summary>
        /// Called every frame from OverlayWindow.OnRender.
        /// </summary>
        /// <param name="myGear">Current gear (-1=R, 0=N, 1+=forward)</param>
        /// <param name="refGear">Reference gear at same lap distance</param>
        /// <param name="engine">DistanceMatchEngine for look-ahead</param>
        /// <param name="lapDist">Current lap distance for look-ahead</param>
        public void Update(int myGear, int refGear, DistanceMatchEngine engine, double lapDist)
        {
            // Display gear as "R", "N", or digit
            GearText.Text = GearLabel(myGear);

            if (!engine.HasReference)
            {
                GearText.Foreground = new SolidColorBrush(Colors.White);
                return;
            }

            if (myGear == refGear)
            {
                GearText.Foreground = new SolidColorBrush(ColorGreen);
                return;
            }

            // Show alert only when ref gear differs and cooldown elapsed
            bool cooldownPassed = (DateTime.Now - _lastAlertTime).TotalSeconds >= AlertCooldownSec;
            if (refGear != _lastAlertGear && cooldownPassed)
            {
                _lastAlertGear = refGear;
                _lastAlertTime = DateTime.Now;

                if (refGear < myGear)
                {
                    // Downshift needed
                    GearText.Foreground = new SolidColorBrush(ColorRed);
                    AlertBackground.Color = ColorRed;
                    AlertText.Text = $"↓ {GearLabel(refGear)}";
                }
                else
                {
                    // Upshift needed
                    GearText.Foreground = new SolidColorBrush(ColorBlue);
                    AlertBackground.Color = ColorBlue;
                    AlertText.Text = $"↑ {GearLabel(refGear)}";
                }

                // Play pulse animation on the gear number
                _pulse.Stop();
                _pulse.Begin();

                // Show and then fade-out the alert
                AlertBorder.Opacity = 1.0;
                _alertFade.Stop();
                _alertFade.Begin();
            }
        }

        private static string GearLabel(int gear) => gear switch
        {
            -1 => "R",
            0  => "N",
            _  => gear.ToString()
        };
    }
}
