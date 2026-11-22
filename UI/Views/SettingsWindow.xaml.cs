using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using LyricIsland.Core.Models;
using LyricIsland.Core.Services;

namespace LyricIsland.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsManager _settingsManager;
    private bool _isWhiteSelected = false;
    private int _selectedTab = 0;
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "lysyn";

    private Color _currentAccentColor = Color.FromRgb(249, 115, 22);
    private Color _currentHighlightColor = Color.FromRgb(251, 146, 60);

    public SettingsWindow(SettingsManager settingsManager)
    {
        InitializeComponent();
        LoadWindowIcon();
        _settingsManager = settingsManager;

        PopulateFontFamilies();
        LoadValuesFromSettings(_settingsManager.CurrentSettings);
        SelectTab(0);

        TimingOffsetSlider.ValueChanged += (_, e) =>
        {
            if (TimingOffsetText != null)
                TimingOffsetText.Text = $"{(int)e.NewValue} ms";
        };
    }

    private void PopulateFontFamilies()
    {
        if (!FontFamilyCombo.Items.Contains("Google Sans"))
        {
            FontFamilyCombo.Items.Add("Google Sans");
        }

        foreach (var font in Fonts.SystemFontFamilies)
        {
            if (!FontFamilyCombo.Items.Contains(font.Source))
            {
                FontFamilyCombo.Items.Add(font.Source);
            }
        }
    }

    private void LoadValuesFromSettings(AppSettings s)
    {
        // GIF Companion Settings (Default: chika.gif)
        string gifPath = !string.IsNullOrWhiteSpace(s.RightCharacter.IdleGifPath)
            ? s.RightCharacter.IdleGifPath
            : "Assets/chika.gif";

        GifPathBox.Text = gifPath;
        GifFlipCheck.IsChecked = s.RightCharacter.FlipHorizontal;
        AnimSpeedSlider.Value = s.AnimationSpeed;
        UpdatePreview(gifPath);

        // Notch & Geometry
        if (s.ThemeHue < 0)
        {
            _isWhiteSelected = true;
            UpdateThemePreview(-1);
        }
        else
        {
            _isWhiteSelected = false;
            HueSlider.Value = s.ThemeHue;
            UpdateThemePreview(s.ThemeHue);
        }

        WidthSlider.Value = s.WidgetWidth;
        HeightSlider.Value = s.WidgetHeight;
        AutoExpandCheck.IsChecked = s.AutoExpandOnHover;
        PinExpandedCheck.IsChecked = s.IsPinned;

        // Typography (Default: Google Sans)
        if (!string.IsNullOrWhiteSpace(s.FontFamily) && FontFamilyCombo.Items.Contains(s.FontFamily))
        {
            FontFamilyCombo.SelectedItem = s.FontFamily;
        }
        else if (FontFamilyCombo.Items.Contains("Google Sans"))
        {
            FontFamilyCombo.SelectedItem = "Google Sans";
        }
        else
        {
            FontFamilyCombo.SelectedItem = "Segoe UI";
        }

        MainFontSizeSlider.Value = s.MainFontSize;

        // Timing Offset
        TimingOffsetSlider.Value = s.TimingOffsetMs;
        TimingOffsetText.Text = $"{s.TimingOffsetMs} ms";

        // Integration
        AlwaysOnTopCheck.IsChecked = s.AlwaysOnTop;
        ClickThroughCheck.IsChecked = s.ClickThrough;
        AutoHideFullscreenCheck.IsChecked = s.AutoHideInFullscreen;
        StartWithWindowsCheck.IsChecked = s.StartWithWindows;

        ToggleHotkeyBox.Text = s.GlobalToggleHotkey;
        ClickThroughHotkeyBox.Text = s.ClickThroughHotkey;
    }

    #region Tab Navigation
    private void NavThemeBtn_Click(object sender, RoutedEventArgs e) => SelectTab(0);
    private void NavGifBtn_Click(object sender, RoutedEventArgs e) => SelectTab(1);
    private void NavNotchBtn_Click(object sender, RoutedEventArgs e) => SelectTab(2);
    private void NavLyricsBtn_Click(object sender, RoutedEventArgs e) => SelectTab(3);
    private void NavIntegrationBtn_Click(object sender, RoutedEventArgs e) => SelectTab(4);

    private void SelectTab(int index)
    {
        _selectedTab = index;

        TabThemeGrid.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        TabGifGrid.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        TabNotchGrid.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        TabLyricsGrid.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        TabIntegrationGrid.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;

        UpdateNavButtonsVisual();
    }

    private void UpdateNavButtonsVisual()
    {
        Button[] navButtons = [NavThemeBtn, NavGifBtn, NavNotchBtn, NavLyricsBtn, NavIntegrationBtn];
        var accentBrush = new SolidColorBrush(_currentAccentColor);
        bool isBright = (_currentAccentColor.R * 0.299 + _currentAccentColor.G * 0.587 + _currentAccentColor.B * 0.114) > 180;
        var activeTextBrush = isBright ? Brushes.Black : Brushes.White;
        var inactiveTextBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170)); // #A1A1AA

        for (int i = 0; i < navButtons.Length; i++)
        {
            var btn = navButtons[i];
            if (i == _selectedTab)
            {
                btn.Background = accentBrush;
                btn.Foreground = activeTextBrush;
            }
            else
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = inactiveTextBrush;
            }
        }
    }
    #endregion

    #region Theming & Recoloring
    private void UpdateThemePreview(double hue)
    {
        Color accent = ThemeColorHelper.GetAccentColor(hue);
        Color highlight = ThemeColorHelper.GetHighlightColor(hue);
        string hex = ThemeColorHelper.ToHex(accent);

        _currentAccentColor = accent;
        _currentHighlightColor = highlight;

        if (HueValueText != null)
        {
            if (hue < 0)
            {
                HueValueText.Text = "White";
                HueValueText.Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245));
            }
            else
            {
                HueValueText.Text = $"{(int)Math.Round(hue)}Â°";
                HueValueText.Foreground = new SolidColorBrush(accent);
            }
        }

        if (ThemeColorSwatch != null)
        {
            ThemeColorSwatch.Background = new SolidColorBrush(accent);
        }

        if (ThemeHexText != null)
        {
            ThemeHexText.Text = hex;
        }

        if (PreviewVisualizerDot != null)
        {
            PreviewVisualizerDot.Fill = new SolidColorBrush(accent);
        }

        if (PreviewPlayBtn != null)
        {
            PreviewPlayBtn.Background = new SolidColorBrush(accent);
        }

        if (PreviewLyricText != null)
        {
            PreviewLyricText.Foreground = new SolidColorBrush(highlight);
        }

        // Dynamically apply selected theme to Settings UI elements
        ApplyThemeToSettings(accent, highlight);

        // Also live-update the notch overlay if currently visible
        if (Application.Current?.MainWindow is OverlayWindow overlay)
        {
            overlay.ApplyTheme(hue, hex, ThemeColorHelper.ToHex(highlight));
        }
    }

    private void ApplyThemeToSettings(Color accent, Color highlight)
    {
        var accentBrush = new SolidColorBrush(accent);
        bool isBright = (accent.R * 0.299 + accent.G * 0.587 + accent.B * 0.114) > 180;
        var highContrastText = isBright ? Brushes.Black : Brushes.White;

        // 1. Header icon / border
        if (HeaderLogoBorder != null)
        {
            HeaderLogoBorder.BorderBrush = accentBrush;
        }

        // 2. Save & Apply button
        if (SaveButton != null)
        {
            SaveButton.Background = accentBrush;
            SaveButton.Foreground = highContrastText;
        }

        // 3. Update sidebar active tab pill
        UpdateNavButtonsVisual();
    }

    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _isWhiteSelected = false;
        UpdateThemePreview(e.NewValue);
    }

    private void SolidRed_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 0; }
    private void SolidOrange_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 30; }
    private void SolidYellow_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 55; }
    private void SolidGreen_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 120; }
    private void SolidCyan_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 180; }
    private void SolidBlue_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 220; }
    private void SolidPurple_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 270; }
    private void SolidPink_Click(object sender, RoutedEventArgs e) { _isWhiteSelected = false; HueSlider.Value = 320; }
    private void SolidWhite_Click(object sender, RoutedEventArgs e)
    {
        _isWhiteSelected = true;
        UpdateThemePreview(-1);
    }
    #endregion

    #region GIF Companion
    private void UpdatePreview(string path)
    {
        try
        {
            PreviewGif.LoadGif(path);
            PreviewPlaceholder.Visibility = PreviewGif.Source != null ? Visibility.Collapsed : Visibility.Visible;
        }
        catch
        {
            PreviewPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void GifPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview(GifPathBox.Text.Trim());
    }

    private void BrowseGif_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "GIF Animations (*.gif)|*.gif|All Animated Images (*.gif;*.webp;*.png)|*.gif;*.webp;*.png|All Files (*.*)|*.*",
            Title = "Select Character GIF"
        };

        if (dlg.ShowDialog() == true)
        {
            GifPathBox.Text = dlg.FileName;
        }
    }

    private void PresetChika_Click(object sender, RoutedEventArgs e) => GifPathBox.Text = "Assets/chika.gif";
    private void PresetArisu_Click(object sender, RoutedEventArgs e) => GifPathBox.Text = "Assets/arisu-aris.gif";
    private void PresetCat_Click(object sender, RoutedEventArgs e) => GifPathBox.Text = "Assets/Characters/cat_playing.gif";
    private void PresetBunny_Click(object sender, RoutedEventArgs e) => GifPathBox.Text = "Assets/Characters/bunny_playing.gif";

    private void GifFlipCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (PreviewGif != null)
        {
            PreviewGif.FlipHorizontal = GifFlipCheck.IsChecked == true;
        }
    }

    private void AnimSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PreviewGif != null)
        {
            PreviewGif.PlaybackSpeed = e.NewValue;
        }
    }
    #endregion

    #region Actions & Saving
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsManager.UpdateSettings(s =>
        {
            string chosenGif = GifPathBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(chosenGif))
            {
                chosenGif = "Assets/chika.gif";
            }

            s.RightCharacter.IdleGifPath = chosenGif;
            s.RightCharacter.PlayingGifPath = chosenGif;
            s.RightCharacter.PausedGifPath = chosenGif;
            s.RightCharacter.FlipHorizontal = GifFlipCheck.IsChecked == true;
            s.AnimationSpeed = AnimSpeedSlider.Value;

            s.WidgetWidth = WidthSlider.Value;
            s.WidgetHeight = HeightSlider.Value;
            s.AutoExpandOnHover = AutoExpandCheck.IsChecked == true;
            s.IsPinned = PinExpandedCheck.IsChecked == true;

            double hue = _isWhiteSelected ? -1 : HueSlider.Value;
            s.ThemeHue = hue;
            s.AccentColorHex = ThemeColorHelper.ToHex(ThemeColorHelper.GetAccentColor(hue));
            s.HighlightColorHex = ThemeColorHelper.ToHex(ThemeColorHelper.GetHighlightColor(hue));

            if (FontFamilyCombo.SelectedItem is string font)
            {
                s.FontFamily = font;
            }

            s.MainFontSize = MainFontSizeSlider.Value;
            s.TimingOffsetMs = (int)TimingOffsetSlider.Value;

            s.AlwaysOnTop = AlwaysOnTopCheck.IsChecked == true;
            s.ClickThrough = ClickThroughCheck.IsChecked == true;
            s.AutoHideInFullscreen = AutoHideFullscreenCheck.IsChecked == true;
            s.StartWithWindows = StartWithWindowsCheck.IsChecked == true;

            s.GlobalToggleHotkey = ToggleHotkeyBox.Text.Trim();
            s.ClickThroughHotkey = ClickThroughHotkeyBox.Text.Trim();
        });

        ApplyStartupRegistry(StartWithWindowsCheck.IsChecked == true);

        Close();
    }

    private static void ApplyStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key != null)
            {
                if (enable)
                {
                    var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppRegistryName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(AppRegistryName) != null)
                    {
                        key.DeleteValue(AppRegistryName, false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set registry startup: {ex.Message}");
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void IncOffsetBtn_Click(object sender, RoutedEventArgs e)
    {
        TimingOffsetSlider.Value = Math.Min(3000, TimingOffsetSlider.Value + 500);
    }

    private void DecOffsetBtn_Click(object sender, RoutedEventArgs e)
    {
        TimingOffsetSlider.Value = Math.Max(-3000, TimingOffsetSlider.Value - 500);
    }

    private void ResetOffsetBtn_Click(object sender, RoutedEventArgs e)
    {
        TimingOffsetSlider.Value = 0;
    }

    private void ClearCacheBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "lysyn",
                "Cache"
            );
            var legacyCacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LyricIsland",
                "Cache"
            );

            int totalDeleted = 0;
            foreach (var dir in new[] { cacheDir, legacyCacheDir })
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.json");
                    foreach (var file in files)
                    {
                        try { File.Delete(file); totalDeleted++; } catch { }
                    }
                }
            }
            MessageBox.Show($"Cleared {totalDeleted} cached lyrics files.", "Cache Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to clear cache: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RecenterButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsManager.UpdateSettings(s =>
        {
            s.WindowLeft = -1;
            s.WindowTop = 0;
        });
        MessageBox.Show("Top notch position reset to top center of screen.", "Position Reset", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Reset all settings to default values?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var defaults = new AppSettings();
            _settingsManager.SaveSettings(defaults);
            LoadValuesFromSettings(defaults);
        }
    }
    #endregion
}
