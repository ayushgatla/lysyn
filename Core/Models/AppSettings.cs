namespace LyricIsland.Core.Models;

public class CharacterConfig
{
    public string IdleGifPath { get; set; } = string.Empty;
    public string PlayingGifPath { get; set; } = string.Empty;
    public string PausedGifPath { get; set; } = string.Empty;
    public string SongChangedGifPath { get; set; } = string.Empty;
    public double Scale { get; set; } = 1.0;
    public bool FlipHorizontal { get; set; } = false;
    public bool Enabled { get; set; } = true;
}

public class AppSettings
{
    // Window layout & geometry
    public double WindowLeft { get; set; } = -1; // -1 means auto center
    public double WindowTop { get; set; } = 0;   // 0 = Pinned to top notch
    public double WidgetWidth { get; set; } = 720;
    public double WidgetHeight { get; set; } = 260;
    public double CollapsedWidth { get; set; } = 280;
    public double CollapsedHeight { get; set; } = 38;
    public bool LockedPosition { get; set; } = false;
    public bool AutoExpandOnHover { get; set; } = true;
    public bool IsPinned { get; set; } = false;

    // Visual appearance (Unified Theme Hue)
    public double Opacity { get; set; } = 0.98;
    public double BackgroundBlurRadius { get; set; } = 24.0;
    public double ThemeHue { get; set; } = -1.0; // Default White Theme (-1)
    public string AccentColorHex { get; set; } = "#F4F4F5"; // Solid White Accent
    public string BackgroundColorHex { get; set; } = "#000000"; // Pitch Black
    public string TextPrimaryColorHex { get; set; } = "#FFFFFF";
    public string TextSecondaryColorHex { get; set; } = "#A1A1AA";
    public string HighlightColorHex { get; set; } = "#FFFFFF";
    public string FontFamily { get; set; } = "Google Sans, Segoe UI, sans-serif";
    public double MainFontSize { get; set; } = 14;
    public double SubFontSize { get; set; } = 11;
    public bool ShowProgressIndicator { get; set; } = true;
    public bool ShowTrackInfo { get; set; } = true;

    // Lyrics timing
    public int TimingOffsetMs { get; set; } = 0; // Manual offset adjustment (+/- ms)

    // Character Companions (Default: chika.gif)
    public CharacterConfig LeftCharacter { get; set; } = new()
    {
        IdleGifPath = "Assets/Characters/cat_idle.gif",
        PlayingGifPath = "Assets/Characters/cat_playing.gif",
        PausedGifPath = "Assets/Characters/cat_paused.gif",
        FlipHorizontal = false,
        Enabled = false // Replaced with circular visualizer on left
    };

    public CharacterConfig RightCharacter { get; set; } = new()
    {
        IdleGifPath = "Assets/chika.gif",
        PlayingGifPath = "Assets/chika.gif",
        PausedGifPath = "Assets/chika.gif",
        FlipHorizontal = false,
        Enabled = true
    };

    public double AnimationSpeed { get; set; } = 1.0;

    // Windows integration
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool AutoHideInFullscreen { get; set; } = true;
    public string GlobalToggleHotkey { get; set; } = "Ctrl+Alt+L";
    public string ClickThroughHotkey { get; set; } = "Ctrl+Alt+C";
}
