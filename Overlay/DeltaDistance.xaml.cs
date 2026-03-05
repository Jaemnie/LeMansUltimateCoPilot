using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace LeMansUltimateCoPilot.Overlay
{
    /// <summary>
    /// Displays the time delta between my current lap time and the reference at the same LapDistance.
    /// Positive (slower than reference) = Red
    /// Negative (faster than reference) = Green
    /// </summary>
    public partial class DeltaDistance : UserControl
    {
        private static readonly Color ColorGreen = Color.FromRgb(0x00, 0xE6, 0x76);
        private static readonly Color ColorRed   = Color.FromRgb(0xFF, 0x52, 0x52);
        private static readonly Color ColorWhite = Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF);

        public DeltaDistance()
        {
            InitializeComponent();
        }

        /// <summary>Called every frame.</summary>
        /// <param name="myLapTime">My elapsed lap time (seconds)</param>
        /// <param name="refLapTime">Reference lap time at same distance (seconds)</param>
        /// <param name="hasReference">False if no reference is loaded</param>
        public void Update(double myLapTime, double refLapTime, bool hasReference)
        {
            if (!hasReference || refLapTime <= 0)
            {
                DeltaText.Text = "---";
                DeltaBrush.Color = ColorWhite;
                SubLabel.Text = "NO REF";
                return;
            }

            double delta = myLapTime - refLapTime;

            DeltaText.Text = delta >= 0
                ? $"+{delta:F3}"
                : $"{delta:F3}";

            DeltaBrush.Color = delta > 0.005 ? ColorRed
                             : delta < -0.005 ? ColorGreen
                             : ColorWhite;

            SubLabel.Text = "vs REF";
        }
    }
}
