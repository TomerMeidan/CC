using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EncoderHub.App.Controls
{
    /// <summary>
    /// A lightweight circular gauge control that renders a needle pointing to a degree value (0–360).
    /// All drawing is done via <see cref="DrawingContext"/> in <see cref="OnRender"/>.
    /// The XAML stub (RadialGauge.xaml) registers the type with the XAML compiler.
    /// </summary>
    public partial class RadialGauge : UserControl
    {
        // ── Dark-theme static resources (frozen once, reused every render) ────────

        private static readonly Brush FaceBrush       = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
        private static readonly Pen   OutlinePen      = new Pen(new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)), 1.5);
        private static readonly Pen   TickPen         = new Pen(new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)), 1.0);
        private static readonly Pen   MiddleTickPen   = new Pen(new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)), 1.0);
        private static readonly Pen   MajorTickPen    = new Pen(new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)), 1.5);
        private static readonly Brush TextBrush       = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
        private static readonly Brush NeedleBrush     = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
        private static readonly Pen   NeedleBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)), 0.5);
        private static readonly Brush ArcBrush        = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1));

        // ── Constructors ──────────────────────────────────────────────────────────

        public RadialGauge()
        {
            InitializeComponent();
        }

        static RadialGauge()
        {
            FaceBrush.Freeze();
            OutlinePen.Freeze();
            TickPen.Freeze();
            MiddleTickPen.Freeze();
            MajorTickPen.Freeze();
            TextBrush.Freeze();
            NeedleBrush.Freeze();
            NeedleBorderPen.Freeze();
            ArcBrush.Freeze();
        }

        // ── Dependency Property ───────────────────────────────────────────────────

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // ── OnRender ──────────────────────────────────────────────────────────────

        protected override void OnRender(DrawingContext dc)
        {
            // Do NOT call base — the empty ControlTemplate has nothing to render.

            double width  = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            double cx     = width  / 2.0;
            double cy     = height / 2.0;
            double radius = Math.Min(cx, cy) - 8.0;
            if (radius <= 0) return;

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // 1. Dark gauge face
            dc.DrawEllipse(FaceBrush, OutlinePen, new Point(cx, cy), radius, radius);

            // 2. Indigo value arc (0° = 12 o'clock, sweeps clockwise to Value°)
            DrawValueArc(dc, cx, cy, radius);

            // 3. Tick marks + degree labels
            double majorTickLen  = radius * 0.10;
            double middleTickLen = radius * 0.07;
            double minorTickLen  = radius * 0.04;
            double textOffset    = radius * 0.22;
            double fontSize      = Math.Max(6.0, radius * 0.090);
            var    typeface      = new Typeface("Segoe UI");

            for (int i = 0; i < 360; i++)
            {
                bool isMajor  = (i % 20 == 0);
                bool isMiddle = !isMajor && (i % 10 == 0);
                double tickLen = isMajor ? majorTickLen : (isMiddle ? middleTickLen : minorTickLen);
                Pen    pen     = isMajor ? MajorTickPen : (isMiddle ? MiddleTickPen : TickPen);

                dc.PushTransform(new RotateTransform(i, cx, cy));
                dc.DrawLine(pen,
                    new Point(cx, cy - radius),
                    new Point(cx, cy - radius + tickLen));

                if (isMajor)
                {
                    var ft = new FormattedText(
                        i.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        TextBrush,
                        pixelsPerDip);

                    dc.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - radius + textOffset));
                }

                dc.Pop();
            }

            // 4. Needle
            dc.PushTransform(new RotateTransform(Value, cx, cy));
            DrawNeedle(dc, cx, cy, radius);
            dc.Pop();

            // 5. Hub cap
            DrawCap(dc, cx, cy, radius);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void DrawValueArc(DrawingContext dc, double cx, double cy, double radius)
        {
            double angle = Value % 360.0;
            if (angle < 0) angle += 360.0;
            if (angle < 0.5) return;

            double arcRadius    = radius * 0.82;
            double arcThickness = radius * 0.055;

            var arcPen = new Pen(ArcBrush, arcThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round
            };

            double startAngleDeg = -90.0;
            double endAngleDeg   = startAngleDeg + angle;
            bool   isLargeArc   = angle > 180.0;

            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad   = endAngleDeg   * Math.PI / 180.0;

            var startPt = new Point(cx + arcRadius * Math.Cos(startRad),
                                    cy + arcRadius * Math.Sin(startRad));
            var endPt   = new Point(cx + arcRadius * Math.Cos(endRad),
                                    cy + arcRadius * Math.Sin(endRad));

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(startPt, isFilled: false, isClosed: false);
                ctx.ArcTo(endPt,
                    new Size(arcRadius, arcRadius),
                    rotationAngle: 0,
                    isLargeArc: isLargeArc,
                    sweepDirection: SweepDirection.Clockwise,
                    isStroked: true,
                    isSmoothJoin: false);
            }
            geo.Freeze();
            arcPen.Freeze();

            dc.DrawGeometry(null, arcPen, geo);
        }

        private static void DrawNeedle(DrawingContext dc, double cx, double cy, double radius)
        {
            double len   = radius * 0.72;
            double base_ = radius * 0.06;

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new Point(cx, cy), isFilled: true, isClosed: true);
                ctx.LineTo(new Point(cx - base_ / 2.0, cy),       isSmoothJoin: true, isStroked: true);
                ctx.LineTo(new Point(cx,                cy - len), isSmoothJoin: true, isStroked: true);
                ctx.LineTo(new Point(cx + base_ / 2.0, cy),       isSmoothJoin: true, isStroked: true);
            }
            geo.Freeze();

            dc.DrawGeometry(NeedleBrush, NeedleBorderPen, geo);
        }

        private static void DrawCap(DrawingContext dc, double cx, double cy, double radius)
        {
            double capR  = radius * 0.11;
            var    brush = new RadialGradientBrush();
            brush.GradientOrigin = new Point(0.35, 0.35);
            brush.Center         = new Point(0.5,  0.5);
            brush.RadiusX        = 0.5;
            brush.RadiusY        = 0.5;
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x47, 0x55, 0x69), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0F, 0x17, 0x2A), 1.0));
            brush.Freeze();

            var capBorder = new Pen(new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)), 1.0);
            capBorder.Freeze();

            dc.DrawEllipse(brush, capBorder, new Point(cx, cy), capR, capR);
        }
    }
}
