using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LyricIsland.Core.Models;
using LyricIsland.Core.Native;
using LyricIsland.Core.Services;

namespace LyricIsland.UI.Views;

public partial class OverlayWindow : Window
{
    private readonly SettingsManager _settingsManager;
    private readonly MediaSessionManager _mediaManager;
    private readonly LyricsService _lyricsService;
    private readonly LyricsSynchronizer _synchronizer;
    private readonly AnimationManager _animationManager;
    private readonly DispatcherTimer _fullscreenTimer;
    private readonly DispatcherTimer _collapseTimer;

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isDragging;
    private bool _isAutoHidden;
    private bool _isExpanded;
    private bool _isPinned;

    private const double CollapsedWidth = 280;
    private const double CollapsedHeight = 44;
    private const double ExpandedWidth = 720;
    private const double ExpandedHeight = 260;

    private bool _hasActiveMedia = false;
    private string _lastCurrentText = string.Empty;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private TimeSpan _currentPosition = TimeSpan.Zero;

    public event Action? OpenSettingsRequested;
    public event Action? ExitRequested;

    public OverlayWindow(
        SettingsManager settingsManager,
        MediaSessionManager mediaManager,
        LyricsService lyricsService,
        LyricsSynchronizer synchronizer,
        AnimationManager animationManager)
    {
        InitializeComponent();
        LoadWindowIcon();

        _settingsManager = settingsManager;
        _mediaManager = mediaManager;
        _lyricsService = lyricsService;
        _synchronizer = synchronizer;
        _animationManager = animationManager;

        _fullscreenTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _fullscreenTimer.Tick += OnFullscreenCheckTick;

        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            if (!_isPinned && !IsMouseOver)
            {
                CollapseNotch();
            }
        };

        ApplySettings(_settingsManager.CurrentSettings);
        _settingsManager.SettingsChanged += OnSettingsChanged;

        Loaded += OnWindowLoaded;
        SourceInitialized += OnSourceInitialized;

        // Subscriptions
        _mediaManager.TrackChanged += OnTrackChanged;
        _mediaManager.PlaybackStateChanged += OnPlaybackStateChanged;
        _mediaManager.TimelinePositionChanged += OnTimelinePositionChanged;
        _synchronizer.Synchronized += OnLyricsSynchronized;
        _animationManager.CharactersUpdated += OnCharactersUpdated;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetToolWindow(_hwnd);
        UpdateClickThrough(_settingsManager.CurrentSettings.ClickThrough);

        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);
    }

    private void LoadWindowIcon()
    {
        try
        {
            string[] candidates = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.ico"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.ico"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "lysyn", "Assets", "logo.ico")
            };
            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    Icon = BitmapFrame.Create(new Uri(p, UriKind.Absolute));
                    break;
                }
            }
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0024) // WM_GETMINMAXINFO
        {
            var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
            mmi.ptMinTrackSize.X = 100;
            mmi.ptMinTrackSize.Y = 10;
            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow(CollapsedWidth, CollapsedHeight);
        _fullscreenTimer.Start();
        OnCharactersUpdated();

        // When nothing is playing initially, notch is completely blank
        if (_mediaManager.CurrentTrack.IsEmpty)
        {
            _hasActiveMedia = false;
            IslandContainer.Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
        else
        {
            _hasActiveMedia = true;
            IslandContainer.Visibility = Visibility.Visible;
            IsHitTestVisible = true;
        }
    }

    private void PositionWindow(double targetWidth, double targetHeight)
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = Math.Max(0, (screenWidth - targetWidth) / 2);
        Top = 0; // Flush with top notch
        Width = targetWidth;
        Height = targetHeight;
    }

    private Color _currentAccentColor = Color.FromRgb(245, 158, 11);
    private Color _currentHighlightColor = Color.FromRgb(254, 240, 138);

    public void ApplySettings(AppSettings settings)
    {
        Dispatcher.Invoke(() =>
        {
            Opacity = settings.Opacity;
            Topmost = settings.AlwaysOnTop;
            _isPinned = settings.IsPinned;

            ApplyTheme(settings.ThemeHue, settings.AccentColorHex, settings.HighlightColorHex);

            _animationManager.UpdateSettings(settings);
            _synchronizer.TimingOffsetMs = settings.TimingOffsetMs;
            RightCharGif.PlaybackSpeed = settings.AnimationSpeed;

            try
            {
                if (!string.IsNullOrWhiteSpace(settings.FontFamily))
                {
                    var family = new FontFamily(settings.FontFamily);
                    CurrentLineText.FontFamily = family;
                    PrevLineText.FontFamily = family;
                    NextLineText.FontFamily = family;
                }
            }
            catch { }

            if (_hwnd != IntPtr.Zero)
            {
                UpdateClickThrough(settings.ClickThrough);
            }
        });
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        ApplySettings(settings);
    }

    public void UpdateClickThrough(bool clickThrough)
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.EnableClickThrough(_hwnd, clickThrough);
        }
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer.Stop();
        if (!_hasActiveMedia) return;
        if (!_isExpanded)
        {
            var pos = e.GetPosition(this);
            if (pos.Y <= CollapsedHeight + 20)
            {
                ExpandNotch();
            }
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPinned) return;
        _collapseTimer.Stop();
        _collapseTimer.Start();
    }

    private void ExpandNotch()
    {
        _isExpanded = true;
        CompactNotchGrid.Visibility = Visibility.Collapsed;
        ExpandedGrid.Visibility = Visibility.Visible;

        AnimateWindow(ExpandedWidth, ExpandedHeight);
    }

    private void CollapseNotch()
    {
        _isExpanded = false;
        ExpandedGrid.Visibility = Visibility.Collapsed;
        CompactNotchGrid.Visibility = Visibility.Visible;

        AnimateWindow(CollapsedWidth, CollapsedHeight);
    }

    private void AnimateWindow(double targetW, double targetH)
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        double targetLeft = Math.Max(0, (screenWidth - targetW) / 2);

        var duration = TimeSpan.FromMilliseconds(200);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        // Clear existing clocks so animations never conflict
        BeginAnimation(WidthProperty, null);
        BeginAnimation(HeightProperty, null);
        BeginAnimation(LeftProperty, null);

        var wAnim = new DoubleAnimation(Width, targetW, duration) { EasingFunction = ease };
        var hAnim = new DoubleAnimation(Height, targetH, duration) { EasingFunction = ease };
        var lAnim = new DoubleAnimation(Left, targetLeft, duration) { EasingFunction = ease };

        hAnim.Completed += (_, _) =>
        {
            BeginAnimation(WidthProperty, null);
            BeginAnimation(HeightProperty, null);
            BeginAnimation(LeftProperty, null);
            Width = targetW;
            Height = targetH;
            Left = targetLeft;
        };

        BeginAnimation(WidthProperty, wAnim);
        BeginAnimation(HeightProperty, hAnim);
        BeginAnimation(LeftProperty, lAnim);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        _settingsManager.UpdateSettings(s => s.IsPinned = _isPinned);
        UpdatePinStateVisual();
    }

    private void UpdatePinStateVisual()
    {
        PinIconText.Foreground = _isPinned
            ? new SolidColorBrush(_currentAccentColor)
            : new SolidColorBrush(Color.FromRgb(113, 113, 122)); // Gray
    }

    public void ApplyTheme(double hue, string? accentHex, string? highlightHex)
    {
        Color accent = ThemeColorHelper.GetAccentColor(hue);
        Color highlight = ThemeColorHelper.GetHighlightColor(hue);

        _currentAccentColor = accent;
        _currentHighlightColor = highlight;

        var accentBrush = new SolidColorBrush(accent);
        var highlightBrush = new SolidColorBrush(highlight);

        // 1. Visualizer
        RadialVisualizer.AccentColor = accent;
        RadialVisualizer.HighlightColor = highlight;

        // 2. Play/Pause squircle button background & glow
        PlayPauseBtn.Background = accentBrush;
        if (Resources["PlayBtnGlow"] is DropShadowEffect playGlow)
        {
            playGlow.Color = accent;
        }

        // 3. Progress bar fill & scrub thumb glow
        ProgressBarFill.Fill = accentBrush;
        ProgressThumb.Fill = accentBrush;
        if (ProgressThumb.Effect is DropShadowEffect thumbGlow)
        {
            thumbGlow.Color = accent;
        }

        // 4. Active lyric line text & glow
        CurrentLineText.Foreground = highlightBrush;
        if (Resources["TextGlow"] is DropShadowEffect textGlow)
        {
            textGlow.Color = highlight;
        }

        // 5. Island ambient glow
        if (Resources["IslandGlow"] is DropShadowEffect islandGlow)
        {
            islandGlow.Color = accent;
        }

        // 6. Generic media logo icons
        CompactGenericIconPath.Fill = accentBrush;
        GenericLogoEllipse.Fill = accentBrush;

        // 7. Update pin state visual with current theme
        UpdatePinStateVisual();
    }

    private void OnCharactersUpdated()
    {
        Dispatcher.Invoke(() =>
        {
            var right = _animationManager.CurrentRightGif;

            if (string.IsNullOrWhiteSpace(right) ||
                right.Contains("chika.gif", StringComparison.OrdinalIgnoreCase) ||
                right.Contains("arisu-aris.gif", StringComparison.OrdinalIgnoreCase))
            {
                string gifName = right?.Contains("arisu", StringComparison.OrdinalIgnoreCase) == true ? "arisu-aris.gif" : "chika.gif";
                string[] candidates = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", gifName),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", gifName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "lysyn", "Assets", gifName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "LyricIsland", "Assets", gifName)
                };
                foreach (var cand in candidates)
                {
                    if (File.Exists(cand))
                    {
                        right = cand;
                        break;
                    }
                }
            }

            if (RightCharGif.GifSource != right)
            {
                RightCharGif.GifSource = right;
                CompactGif.GifSource = right;
            }
            RightCharGif.FlipHorizontal = _animationManager.RightFlip;
        });
    }

    private void OnTrackChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(async () =>
        {
            _currentDuration = track.Duration;

            if (track.IsEmpty)
            {
                _hasActiveMedia = false;
                if (_isExpanded)
                {
                    CollapseNotch();
                }
                IslandContainer.Visibility = Visibility.Collapsed;
                IsHitTestVisible = false;
                CompactLyricText.Text = string.Empty;
                ExpandedTitleText.Text = string.Empty;
                ExpandedArtistText.Text = string.Empty;
                SourceAppBadgeText.Text = string.Empty;
                RadialVisualizer.AlbumArtSource = null;
                RadialVisualizer.IsPlaying = false;
                _synchronizer.SetLyrics(null, TimeSpan.Zero);
                return;
            }

            _hasActiveMedia = true;
            IslandContainer.Visibility = Visibility.Visible;
            IsHitTestVisible = true;

            CompactLyricText.Text = $"{track.Title} • {track.Artist}";
            ExpandedTitleText.Text = track.Title;
            ExpandedArtistText.Text = string.IsNullOrWhiteSpace(track.Album)
                ? track.Artist
                : $"{track.Artist} • {track.Album}";

            UpdateBrandBadge(track);

            // Album Artwork
            if (track.ThumbnailBytes != null && track.ThumbnailBytes.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(track.ThumbnailBytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    RadialVisualizer.AlbumArtSource = bmp;
                }
                catch
                {
                    RadialVisualizer.AlbumArtSource = null;
                }
            }
            else
            {
                RadialVisualizer.AlbumArtSource = null;
            }

            _animationManager.OnSongChanged();

            // Fetch lyrics asynchronously
            var lyrics = await _lyricsService.GetLyricsAsync(track);
            _synchronizer.SetLyrics(lyrics, track.Duration);
        });
    }

    private void UpdateBrandBadge(MediaTrackInfo track)
    {
        SpotifyLogoGrid.Visibility = Visibility.Collapsed;
        YouTubeStandardLogoGrid.Visibility = Visibility.Collapsed;
        YouTubeMusicLogoGrid.Visibility = Visibility.Collapsed;
        AppleLogoGrid.Visibility = Visibility.Collapsed;
        GenericLogoGrid.Visibility = Visibility.Collapsed;

        CompactSpotifyLogoGrid.Visibility = Visibility.Collapsed;
        CompactYouTubeStandardLogoGrid.Visibility = Visibility.Collapsed;
        CompactYouTubeMusicLogoGrid.Visibility = Visibility.Collapsed;
        CompactAppleLogoGrid.Visibility = Visibility.Collapsed;
        CompactGenericLogoGrid.Visibility = Visibility.Collapsed;

        var detected = MediaSourceDetector.Detect(track);
        SourceAppBadgeText.Text = detected.DisplayName;

        switch (detected.Type)
        {
            case MediaSourceType.Spotify:
                SpotifyLogoGrid.Visibility = Visibility.Visible;
                CompactSpotifyLogoGrid.Visibility = Visibility.Visible;
                break;

            case MediaSourceType.YouTube:
                YouTubeStandardLogoGrid.Visibility = Visibility.Visible;
                CompactYouTubeStandardLogoGrid.Visibility = Visibility.Visible;
                break;

            case MediaSourceType.YouTubeMusic:
                YouTubeMusicLogoGrid.Visibility = Visibility.Visible;
                CompactYouTubeMusicLogoGrid.Visibility = Visibility.Visible;
                break;

            case MediaSourceType.AppleMusic:
                AppleLogoGrid.Visibility = Visibility.Visible;
                CompactAppleLogoGrid.Visibility = Visibility.Visible;
                break;

            default:
                GenericLogoGrid.Visibility = Visibility.Visible;
                CompactGenericLogoGrid.Visibility = Visibility.Visible;
                break;
        }
    }

    private void OnPlaybackStateChanged(PlaybackState state)
    {
        Dispatcher.Invoke(() =>
        {
            _animationManager.OnPlaybackStateChanged(state);
            _synchronizer.UpdatePlaybackState(state, _mediaManager.CurrentTrack.Position);

            bool isPlaying = state == PlaybackState.Playing;
            RadialVisualizer.IsPlaying = isPlaying;
            PlayPath.Visibility = isPlaying ? Visibility.Collapsed : Visibility.Visible;
            PausePath.Visibility = isPlaying ? Visibility.Visible : Visibility.Collapsed;

            if (isPlaying)
            {
                _hasActiveMedia = true;
                IslandContainer.Visibility = Visibility.Visible;
                IsHitTestVisible = true;
                RightCharGif.Resume();
                CompactGif.Resume();
            }
            else
            {
                RightCharGif.Pause();
                CompactGif.Pause();

                if (state == PlaybackState.Stopped && _mediaManager.CurrentTrack.IsEmpty)
                {
                    _hasActiveMedia = false;
                    if (_isExpanded) CollapseNotch();
                    IslandContainer.Visibility = Visibility.Collapsed;
                    IsHitTestVisible = false;
                }
            }
        });
    }

    private void OnTimelinePositionChanged(TimeSpan position)
    {
        Dispatcher.Invoke(() =>
        {
            _currentPosition = position;
            _synchronizer.SyncTimelinePosition(position);
        });
    }

    private void OnLyricsSynchronized(object? sender, LyricSyncEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _currentPosition = e.CurrentPosition;
            if (e.TotalDuration.TotalSeconds > 0)
            {
                _currentDuration = e.TotalDuration;
            }

            if (e.HasSyncedLyrics)
            {
                var curText = e.CurrentLine?.Text ?? string.Empty;
                if (_lastCurrentText != curText)
                {
                    _lastCurrentText = curText;
                    CurrentLineText.Text = string.IsNullOrWhiteSpace(curText) ? "•••" : curText;
                    if (!string.IsNullOrWhiteSpace(curText))
                    {
                        CompactLyricText.Text = curText;
                    }
                }

                PrevLineText.Text = e.PreviousLine?.Text ?? string.Empty;
                NextLineText.Text = e.NextLine?.Text ?? string.Empty;
            }
            else
            {
                CurrentLineText.Text = string.IsNullOrWhiteSpace(e.FallbackText) ? "•••" : e.FallbackText;
                PrevLineText.Text = string.Empty;
                NextLineText.Text = string.Empty;
            }

            // Timestamps
            ElapsedText.Text = FormatTime(_currentPosition);
            TotalTimeText.Text = FormatTime(_currentDuration);

            // Progress Bar
            if (_currentDuration.TotalSeconds > 0)
            {
                var parentGrid = ProgressBarFill.Parent as FrameworkElement;
                double totalWidth = parentGrid != null && parentGrid.ActualWidth > 0 ? parentGrid.ActualWidth : 280;
                double ratio = Math.Clamp(_currentPosition.TotalSeconds / _currentDuration.TotalSeconds, 0.0, 1.0);
                ProgressBarFill.Width = totalWidth * ratio;
                ProgressThumb.Margin = new Thickness((totalWidth * ratio) - 5, 0, 0, 0);
            }
            else
            {
                ProgressBarFill.Width = 0;
                ProgressThumb.Margin = new Thickness(-5, 0, 0, 0);
            }
        });
    }

    private static string FormatTime(TimeSpan ts)
    {
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private async void PrevTrackBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaManager.SkipPreviousAsync();
    }

    private async void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaManager.TogglePlayPauseAsync();
    }

    private async void NextTrackBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaManager.SkipNextAsync();
    }

    private async void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentDuration.TotalSeconds <= 0) return;

        var elem = sender as FrameworkElement;
        double totalWidth = elem != null && elem.ActualWidth > 0 ? elem.ActualWidth : 280;
        var pos = e.GetPosition(elem);
        double ratio = Math.Clamp(pos.X / totalWidth, 0.0, 1.0);
        var targetPosition = TimeSpan.FromSeconds(_currentDuration.TotalSeconds * ratio);

        await _mediaManager.ChangePlaybackPositionAsync(targetPosition);
    }

    private void OnFullscreenCheckTick(object? sender, EventArgs e)
    {
        if (!_settingsManager.CurrentSettings.AutoHideInFullscreen)
        {
            if (_isAutoHidden)
            {
                _isAutoHidden = false;
                if (_hasActiveMedia)
                {
                    Visibility = Visibility.Visible;
                    IslandContainer.Visibility = Visibility.Visible;
                    RightCharGif.Resume();
                }
            }
            return;
        }

        bool isFullscreen = FullscreenDetector.IsForegroundAppFullscreen();
        if (isFullscreen && !_isAutoHidden)
        {
            _isAutoHidden = true;
            Visibility = Visibility.Hidden;
            RightCharGif.Pause();
        }
        else if (!isFullscreen && _isAutoHidden)
        {
            _isAutoHidden = false;
            if (_hasActiveMedia)
            {
                Visibility = Visibility.Visible;
                IslandContainer.Visibility = Visibility.Visible;
                RightCharGif.Resume();
            }
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settingsManager.CurrentSettings.LockedPosition) return;
        if (e.ButtonState == MouseButtonState.Pressed && !_isExpanded)
        {
            _isDragging = true;
            DragMove();
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _settingsManager.UpdateSettings(s =>
            {
                s.WindowLeft = Left;
                s.WindowTop = Top;
            });
        }
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙ Settings..." };
        settingsItem.Click += (_, _) => OpenSettingsRequested?.Invoke();
        menu.Items.Add(settingsItem);

        var pinItem = new System.Windows.Controls.MenuItem
        {
            Header = _isPinned ? "📌 Unpin Notch" : "📌 Pin Notch (Keep Expanded)"
        };
        pinItem.Click += (_, _) =>
        {
            _isPinned = !_isPinned;
            _settingsManager.UpdateSettings(s => s.IsPinned = _isPinned);
            UpdatePinStateVisual();
            if (!_isPinned && !IsMouseOver) CollapseNotch();
        };
        menu.Items.Add(pinItem);

        var clickThroughItem = new System.Windows.Controls.MenuItem
        {
            Header = _settingsManager.CurrentSettings.ClickThrough ? "Disable Click-Through" : "Enable Click-Through"
        };
        clickThroughItem.Click += (_, _) =>
        {
            _settingsManager.UpdateSettings(s => s.ClickThrough = !s.ClickThrough);
        };
        menu.Items.Add(clickThroughItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "🔄 Refresh Lyrics / Session" };
        refreshItem.Click += async (_, _) =>
        {
            await _mediaManager.RefreshSessionDetailsAsync();
        };
        menu.Items.Add(refreshItem);

        var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ Exit lysyn" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _fullscreenTimer.Stop();
        _collapseTimer.Stop();
        base.OnClosed(e);
    }
}
