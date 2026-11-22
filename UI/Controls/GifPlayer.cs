using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LyricIsland.UI.Controls;

public class GifFrame
{
    public ImageSource Image { get; }
    public TimeSpan Delay { get; }

    public GifFrame(ImageSource image, TimeSpan delay)
    {
        Image = image;
        Delay = delay;
    }
}

public class GifPlayer : System.Windows.Controls.Image
{
    private readonly List<GifFrame> _frames = new();
    private readonly DispatcherTimer _frameTimer;
    private int _currentFrameIndex;
    private bool _isAnimated;
    private bool _isPaused;

    public static readonly DependencyProperty GifSourceProperty =
        DependencyProperty.Register(
            nameof(GifSource),
            typeof(string),
            typeof(GifPlayer),
            new PropertyMetadata(string.Empty, OnGifSourceChanged));

    public string GifSource
    {
        get => (string)GetValue(GifSourceProperty);
        set => SetValue(GifSourceProperty, value);
    }

    public static readonly DependencyProperty FlipHorizontalProperty =
        DependencyProperty.Register(
            nameof(FlipHorizontal),
            typeof(bool),
            typeof(GifPlayer),
            new PropertyMetadata(false, OnFlipHorizontalChanged));

    public bool FlipHorizontal
    {
        get => (bool)GetValue(FlipHorizontalProperty);
        set => SetValue(FlipHorizontalProperty, value);
    }

    public static readonly DependencyProperty PlaybackSpeedProperty =
        DependencyProperty.Register(
            nameof(PlaybackSpeed),
            typeof(double),
            typeof(GifPlayer),
            new PropertyMetadata(1.0, OnPlaybackSpeedChanged));

    public double PlaybackSpeed
    {
        get => (double)GetValue(PlaybackSpeedProperty);
        set => SetValue(PlaybackSpeedProperty, value);
    }

    public GifPlayer()
    {
        _frameTimer = new DispatcherTimer(DispatcherPriority.Render);
        _frameTimer.Tick += OnFrameTimerTick;

        Loaded += (_, _) => CheckAnimationState();
        Unloaded += (_, _) => StopAnimation();
        IsVisibleChanged += (_, _) => CheckAnimationState();
    }

    private static void OnGifSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GifPlayer player)
        {
            player.LoadGif(e.NewValue as string);
        }
    }

    private static void OnFlipHorizontalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GifPlayer player)
        {
            var flip = (bool)e.NewValue;
            player.RenderTransformOrigin = new Point(0.5, 0.5);
            player.RenderTransform = new ScaleTransform(flip ? -1 : 1, 1);
        }
    }

    private static void OnPlaybackSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GifPlayer player && player._isAnimated && !player._isPaused)
        {
            player.ScheduleNextFrame();
        }
    }

    public void LoadGif(string? path)
    {
        StopAnimation();
        _frames.Clear();
        _currentFrameIndex = 0;
        _isAnimated = false;

        if (string.IsNullOrWhiteSpace(path))
        {
            Source = null;
            return;
        }

        try
        {
            Uri uri;
            if (File.Exists(path))
            {
                uri = new Uri(Path.GetFullPath(path));
            }
            else
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var combined = Path.Combine(baseDir, path);
                if (File.Exists(combined))
                {
                    uri = new Uri(combined);
                }
                else
                {
                    // Attempt pack or relative URI
                    if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out uri!))
                    {
                        return;
                    }
                }
            }

            var decoder = BitmapDecoder.Create(
                uri,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count <= 1)
            {
                if (decoder.Frames.Count == 1)
                {
                    Source = decoder.Frames[0];
                }
                return;
            }

            foreach (var frame in decoder.Frames)
            {
                var delay = GetFrameDelay(frame);
                var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
                converted.Freeze();
                _frames.Add(new GifFrame(converted, delay));
            }

            _isAnimated = _frames.Count > 1;
            if (_frames.Count > 0)
            {
                Source = _frames[0].Image;
            }

            CheckAnimationState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GifPlayer failed to load '{path}': {ex.Message}");
            Source = null;
        }
    }

    private static TimeSpan GetFrameDelay(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata metadata &&
                metadata.ContainsQuery("/grctlext/Delay"))
            {
                var delayVal = metadata.GetQuery("/grctlext/Delay");
                if (delayVal is ushort delayCentiseconds && delayCentiseconds > 0)
                {
                    return TimeSpan.FromMilliseconds(delayCentiseconds * 10);
                }
                if (delayVal is int delayInt && delayInt > 0)
                {
                    return TimeSpan.FromMilliseconds(delayInt * 10);
                }
            }
        }
        catch { }

        return TimeSpan.FromMilliseconds(100); // 10 FPS fallback
    }

    public void Pause()
    {
        _isPaused = true;
        _frameTimer.Stop();
    }

    public void Resume()
    {
        _isPaused = false;
        CheckAnimationState();
    }

    private void CheckAnimationState()
    {
        if (!_isAnimated || _isPaused || !IsVisible || !IsLoaded || _frames.Count == 0)
        {
            _frameTimer.Stop();
            return;
        }

        if (!_frameTimer.IsEnabled)
        {
            ScheduleNextFrame();
        }
    }

    private void StopAnimation()
    {
        _frameTimer.Stop();
    }

    private void OnFrameTimerTick(object? sender, EventArgs e)
    {
        _frameTimer.Stop();
        if (_frames.Count == 0) return;

        _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Count;
        Source = _frames[_currentFrameIndex].Image;

        if (_isAnimated && !_isPaused && IsVisible)
        {
            ScheduleNextFrame();
        }
    }

    private void ScheduleNextFrame()
    {
        if (_frames.Count == 0) return;

        var baseDelay = _frames[_currentFrameIndex].Delay;
        var speed = Math.Clamp(PlaybackSpeed, 0.25, 4.0);
        var actualMs = Math.Max(15, baseDelay.TotalMilliseconds / speed);

        _frameTimer.Interval = TimeSpan.FromMilliseconds(actualMs);
        _frameTimer.Start();
    }
}
