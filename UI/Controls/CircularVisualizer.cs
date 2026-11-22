using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LyricIsland.UI.Controls;

public class CircularVisualizer : FrameworkElement
{
    private readonly DispatcherTimer _animTimer;
    private double _angleOffset;
    private double _pulsePhase;

    public static readonly DependencyProperty AlbumArtSourceProperty =
        DependencyProperty.Register(
            nameof(AlbumArtSource),
            typeof(ImageSource),
            typeof(CircularVisualizer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public ImageSource? AlbumArtSource
    {
        get => (ImageSource?)GetValue(AlbumArtSourceProperty);
        set => SetValue(AlbumArtSourceProperty, value);
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(CircularVisualizer),
            new PropertyMetadata(false, OnIsPlayingChanged));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(
            nameof(AccentColor),
            typeof(Color),
            typeof(CircularVisualizer),
            new FrameworkPropertyMetadata(Color.FromRgb(245, 158, 11), FrameworkPropertyMetadataOptions.AffectsRender));

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public static readonly DependencyProperty HighlightColorProperty =
        DependencyProperty.Register(
            nameof(HighlightColor),
            typeof(Color),
            typeof(CircularVisualizer),
            new FrameworkPropertyMetadata(Color.FromRgb(254, 240, 138), FrameworkPropertyMetadataOptions.AffectsRender));

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    public CircularVisualizer()
    {
        _animTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
        };
        _animTimer.Tick += OnAnimTimerTick;

        Loaded += (_, _) => UpdateTimerState();
        Unloaded += (_, _) => _animTimer.Stop();
        IsVisibleChanged += (_, _) => UpdateTimerState();
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularVisualizer visualizer)
        {
            visualizer.UpdateTimerState();
            visualizer.InvalidateVisual();
        }
    }

    private void UpdateTimerState()
    {
        if (IsPlaying && IsVisible && IsLoaded)
        {
            if (!_animTimer.IsEnabled) _animTimer.Start();
        }
        else
        {
            _animTimer.Stop();
        }
    }

    private void OnAnimTimerTick(object? sender, EventArgs e)
    {
        _angleOffset = (_angleOffset + 1.2) % 360;
        _pulsePhase += 0.15;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var center = new Point(w / 2, h / 2);
        double outerRadius = Math.Min(w, h) / 2 - 4;
        double innerCircleRadius = outerRadius - 16;
        if (innerCircleRadius < 10) return;

        // 1. Draw outer radial dots/bars (inspired by screenshot)
        const int dotCount = 36;
        var dotBrush = new SolidColorBrush(AccentColor);
        var highlightDotBrush = new SolidColorBrush(HighlightColor);

        double ringRadius = innerCircleRadius + 8;

        for (int i = 0; i < dotCount; i++)
        {
            double angleDeg = _angleOffset + (i * 360.0 / dotCount);
            double angleRad = angleDeg * Math.PI / 180.0;

            // Animate dot radius with rhythmic pulse when playing
            double dotR = 2.0;
            if (IsPlaying)
            {
                double wave = Math.Sin(_pulsePhase + (i * 0.45));
                dotR = 1.8 + Math.Max(0, wave * 1.5);
            }

            double x = center.X + Math.Cos(angleRad) * ringRadius;
            double y = center.Y + Math.Sin(angleRad) * ringRadius;

            // All dots use the selected theme color - no stray red dots!
            Brush currentBrush = (dotR > 2.6) ? highlightDotBrush : dotBrush;
            dc.DrawEllipse(currentBrush, null, new Point(x, y), dotR, dotR);
        }

        // 2. Draw inner dark pod/disk
        var podBrush = new SolidColorBrush(Color.FromRgb(8, 8, 8)); // Deep pitch black
        var podBorderPen = new Pen(new SolidColorBrush(Color.FromArgb(90, AccentColor.R, AccentColor.G, AccentColor.B)), 1.2);
        dc.DrawEllipse(podBrush, podBorderPen, center, innerCircleRadius, innerCircleRadius);

        // 3. Draw Album Art or Fallback Icon
        if (AlbumArtSource != null)
        {
            var clipGeo = new EllipseGeometry(center, innerCircleRadius - 2, innerCircleRadius - 2);
            dc.PushClip(clipGeo);

            var rect = new Rect(
                center.X - innerCircleRadius + 2,
                center.Y - innerCircleRadius + 2,
                (innerCircleRadius - 2) * 2,
                (innerCircleRadius - 2) * 2);

            dc.DrawImage(AlbumArtSource, rect);
            dc.Pop();
        }
        else
        {
            // Draw sleek cyber mask / music icon matching user's image
            DrawFallbackCenterIcon(dc, center, innerCircleRadius, AccentColor);
        }

        // 4. Subtle inner ring highlight
        var innerRingPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0.8);
        dc.DrawEllipse(null, innerRingPen, center, innerCircleRadius - 2, innerCircleRadius - 2);
    }

    private static void DrawFallbackCenterIcon(DrawingContext dc, Point center, double radius, Color accent)
    {
        var iconBrush = new SolidColorBrush(Color.FromRgb(240, 240, 245));
        var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(120, accent.R, accent.G, accent.B)), 1.2);

        double s = radius * 0.45;

        // Head outline (skull / cyberpunk mask)
        var headRect = new Rect(center.X - s * 0.65, center.Y - s * 0.75, s * 1.3, s * 1.2);
        dc.DrawRoundedRectangle(null, new Pen(iconBrush, 1.5), headRect, s * 0.4, s * 0.4);

        // Eyes
        double eyeW = s * 0.28;
        double eyeH = s * 0.22;
        dc.DrawEllipse(iconBrush, null, new Point(center.X - s * 0.32, center.Y - s * 0.15), eyeW, eyeH);
        dc.DrawEllipse(iconBrush, null, new Point(center.X + s * 0.32, center.Y - s * 0.15), eyeW, eyeH);

        // Jaw / teeth lines
        var jawPen = new Pen(iconBrush, 1.2);
        dc.DrawLine(jawPen, new Point(center.X - s * 0.25, center.Y + s * 0.28), new Point(center.X + s * 0.25, center.Y + s * 0.28));
        dc.DrawLine(jawPen, new Point(center.X - s * 0.15, center.Y + s * 0.18), new Point(center.X - s * 0.15, center.Y + s * 0.35));
        dc.DrawLine(jawPen, new Point(center.X + s * 0.15, center.Y + s * 0.18), new Point(center.X + s * 0.15, center.Y + s * 0.35));

        // Subtle accent line at bottom using theme accent color
        var accentPen = new Pen(new SolidColorBrush(accent), 1.0);
        dc.DrawLine(accentPen, new Point(center.X - s * 0.2, center.Y + s * 0.55), new Point(center.X + s * 0.2, center.Y + s * 0.55));
    }
}
