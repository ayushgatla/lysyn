using System;
using System.Windows;
using System.Windows.Interop;
using LyricIsland.Assets;
using LyricIsland.Core.Native;
using LyricIsland.Core.Services;
using LyricIsland.UI.Tray;
using LyricIsland.UI.Views;

namespace LyricIsland;

public partial class App : System.Windows.Application
{
    private SettingsManager? _settingsManager;
    private MediaSessionManager? _mediaSessionManager;
    private LyricsService? _lyricsService;
    private LyricsSynchronizer? _lyricsSynchronizer;
    private AnimationManager? _animationManager;
    private SystemTrayManager? _trayManager;
    private HotkeyManager? _hotkeyManager;
    private OverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure default bundled character GIFs exist
        AssetGenerator.EnsureDefaultCharactersExist();

        // Initialize core services
        _settingsManager = new SettingsManager();
        _lyricsService = new LyricsService();
        _lyricsSynchronizer = new LyricsSynchronizer();
        _animationManager = new AnimationManager(_settingsManager.CurrentSettings);
        _mediaSessionManager = new MediaSessionManager();

        _trayManager = new SystemTrayManager(_settingsManager);
        _trayManager.ToggleVisibilityRequested += OnToggleVisibility;
        _trayManager.SettingsRequested += OnOpenSettings;
        _trayManager.RefreshRequested += OnRefreshRequested;
        _trayManager.ExitRequested += OnExitRequested;

        // Initialize UI overlay
        _overlayWindow = new OverlayWindow(
            _settingsManager,
            _mediaSessionManager,
            _lyricsService,
            _lyricsSynchronizer,
            _animationManager);

        _overlayWindow.OpenSettingsRequested += OnOpenSettings;
        _overlayWindow.ExitRequested += OnExitRequested;
        _overlayWindow.Show();

        // Register Global Hotkeys & Tray Icon
        var hwnd = new WindowInteropHelper(_overlayWindow).Handle;
        if (hwnd != IntPtr.Zero)
        {
            _trayManager.Initialize(hwnd);

            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.ToggleVisibilityRequested += OnToggleVisibility;
            _hotkeyManager.ToggleClickThroughRequested += OnToggleClickThrough;
            _hotkeyManager.Register(
                hwnd,
                _settingsManager.CurrentSettings.GlobalToggleHotkey,
                _settingsManager.CurrentSettings.ClickThroughHotkey);
        }

        // Initialize SMTC media session detection
        await _mediaSessionManager.InitializeAsync();

        // Memory optimization: Trim initial JIT/WPF working set to achieve low RAM footprint
        _ = Task.Run(async () =>
        {
            await Task.Delay(2500);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            NativeMethods.EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle);
        });
    }

    private void OnToggleVisibility()
    {
        if (_overlayWindow == null) return;

        if (_overlayWindow.Visibility == Visibility.Visible)
        {
            _overlayWindow.Visibility = Visibility.Collapsed;
        }
        else
        {
            _overlayWindow.Visibility = Visibility.Visible;
            _overlayWindow.Activate();
        }
    }

    private void OnToggleClickThrough()
    {
        if (_settingsManager == null) return;
        _settingsManager.UpdateSettings(s => s.ClickThrough = !s.ClickThrough);
    }

    private void OnOpenSettings()
    {
        if (_settingsManager == null) return;

        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(_settingsManager);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private async void OnRefreshRequested()
    {
        if (_mediaSessionManager != null)
        {
            await _mediaSessionManager.RefreshSessionDetailsAsync();
        }
    }

    private void OnExitRequested()
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _fullscreenTimerDispose();
        _hotkeyManager?.Dispose();
        _trayManager?.Dispose();
        _overlayWindow?.Close();
        _mediaSessionManager?.Dispose();
        _lyricsSynchronizer?.Dispose();
        _lyricsService?.Dispose();
        _animationManager?.Dispose();

        base.OnExit(e);
    }

    private void _fullscreenTimerDispose()
    {
        // Handled by window closed
    }
}
