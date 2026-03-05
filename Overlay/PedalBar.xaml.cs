using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LeMansUltimateCoPilot.Overlay
{
    /// <summary>
    /// Progressive bar showing my pedal input (filled bar) vs reference (line marker).
    /// Color logic:
    ///   Green  - within 5% of reference (matching)
    ///   Blue   - my input is less than reference (need to press more)
    ///   Red    - my input exceeds reference (over-pressing)
    /// </summary>
    public partial class PedalBar : UserControl
    {
        // Max usable height inside the bar area (grid minus margins)
        private const double MaxBarHeight = 190.0;

        // Colors
        private static readonly Color ColorGreen = Color.FromRgb(0x00, 0xE6, 0x76);
        private static readonly Color ColorBlue  = Color.FromRgb(0x40, 0xC4, 0xFF);
        private static readonly Color ColorRed   = Color.FromRgb(0xFF, 0x52, 0x52);

        public string Label
        {
            get => LabelText.Text;
            set => LabelText.Text = value;
        }

        public PedalBar()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called every frame from OverlayWindow.OnRender.
        /// </summary>
        /// <param name="myValue">My current pedal input 0.0-1.0</param>
        /// <param name="refValue">Reference pedal input 0.0-1.0</param>
        /// <param name="isPedalBrake">True for brake (ref bar = upper = dangerous), False for throttle</param>
        public void Update(float myValue, float refValue, bool isPedalBrake)
        {
            myValue  = Math.Clamp(myValue, 0f, 1f);
            refValue = Math.Clamp(refValue, 0f, 1f);

            double myHeight  = myValue  * MaxBarHeight;
            double refHeight = refValue * MaxBarHeight;

            // Update bar heights
            MyBar.Height    = myHeight;
            RefMarker.Margin = new Thickness(2, 0, 2, refHeight);

            // Determine delta color
            float delta = myValue - refValue;
            Color barColor;

            if (Math.Abs(delta) < 0.05f)
                barColor = ColorGreen;
            else if (delta < 0)
                barColor = ColorBlue;  // less than reference
            else
                barColor = ColorRed;   // more than reference

            MyBarBrush.Color = barColor;

            // Percentage label
            ValueText.Text = $"{myValue * 100:F0}%";
        }
    }
}
