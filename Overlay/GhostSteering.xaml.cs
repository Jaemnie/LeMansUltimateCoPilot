using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LeMansUltimateCoPilot.Overlay
{
    /// <summary>
    /// Steering wheel guide.
    /// - Reference: dashed white arc showing target angle.
    /// - My input: solid colored arc showing current angle.
    /// Color logic (same 3-color scheme):
    ///   Green  - within 5% of reference
    ///   Blue   - my steering is less (need to turn more)
    ///   Red    - my steering is more (risk of oversteer)
    /// Steering value -1.0 (full left) to +1.0 (full right) maps to -90 to +90 degrees.
    /// </summary>
    public partial class GhostSteering : UserControl
    {
        private static readonly Color ColorGreen = Color.FromRgb(0x00, 0xE6, 0x76);
        private static readonly Color ColorBlue  = Color.FromRgb(0x40, 0xC4, 0xFF);
        private static readonly Color ColorRed   = Color.FromRgb(0xFF, 0x52, 0x52);

        // Wheel geometry constants (canvas coordinates)
        private const double CenterX = 90;
        private const double CenterY = 73;  // hub position
        private const double Radius = 55;
        private const double MaxAngleDeg = 90.0;

        public GhostSteering()
        {
            InitializeComponent();
        }

        /// <summary>Called every frame from OverlayWindow.OnRender.</summary>
        /// <param name="mySteering">My current steering -1.0 to 1.0</param>
        /// <param name="refSteering">Reference steering -1.0 to 1.0</param>
        public void Update(float mySteering, float refSteering)
        {
            mySteering  = Math.Clamp(mySteering, -1f, 1f);
            refSteering = Math.Clamp(refSteering, -1f, 1f);

            // Draw arcs
            RefArc.Data  = BuildArcGeometry(refSteering, Radius + 2);
            MyArc.Data   = BuildArcGeometry(mySteering,  Radius);

            // Color
            float delta = mySteering - refSteering;
            Color c;
            if (Math.Abs(delta) < 0.05f)
                c = ColorGreen;
            else if (Math.Abs(mySteering) < Math.Abs(refSteering))
                c = ColorBlue;
            else
                c = ColorRed;

            MyArcBrush.Color = c;
        }

        // Builds a semicircular arc rotated by the steering angle.
        private static Geometry BuildArcGeometry(float steering, double radius)
        {
            // Angle of rotation from center-up (0°) based on steering input
            double rotDeg = steering * MaxAngleDeg;
            double rotRad = rotDeg * Math.PI / 180.0;

            // We draw a fixed semicircle (180° arc) rotated by the steering angle.
            // Arc starts at left side, ends at right side.
            double arcHalfAngle = 75.0 * Math.PI / 180.0; // 150° total arc

            double startAngle = -Math.PI / 2 - arcHalfAngle + rotRad;
            double endAngle   = -Math.PI / 2 + arcHalfAngle + rotRad;

            var startPt = ArcPoint(startAngle, radius);
            var endPt   = ArcPoint(endAngle, radius);

            // Build path geometry (large arc flag = true since > 180° would need it, ours is 150°)
            var seg = new ArcSegment
            {
                Point = endPt,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            };

            var figure = new PathFigure { StartPoint = startPt, IsClosed = false };
            figure.Segments.Add(seg);

            return new PathGeometry(new[] { figure });
        }

        private static Point ArcPoint(double angle, double radius) =>
            new Point(CenterX + radius * Math.Cos(angle),
                      CenterY + radius * Math.Sin(angle));
    }
}
