using System;
using System.IO;
using System.Text.Json;
using LyricIsland.Core.Models;

namespace LyricIsland.Core.Services;

public class SettingsManager
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "lysyn"
    );

    private static readonly string LegacyAppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LyricIsland"
    );

    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");
    private static readonly string LegacySettingsFilePath = Path.Combine(LegacyAppDataFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings CurrentSettings { get; private set; }

    public event Action<AppSettings>? SettingsChanged;

    public SettingsManager()
    {
        CurrentSettings = LoadSettings();
    }

    public AppSettings LoadSettings()
    {
        try
        {
            // Migrate legacy settings file if lysyn settings doesn't exist yet
            if (!File.Exists(SettingsFilePath) && File.Exists(LegacySettingsFilePath))
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
                File.Copy(LegacySettingsFilePath, SettingsFilePath, true);
            }

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    // Ensure valid companion GIF
                    if (string.IsNullOrWhiteSpace(settings.RightCharacter.IdleGifPath) ||
                        settings.RightCharacter.IdleGifPath.Contains("bunny", StringComparison.OrdinalIgnoreCase))
                    {
                        settings.RightCharacter.IdleGifPath = "Assets/chika.gif";
                        settings.RightCharacter.PlayingGifPath = "Assets/chika.gif";
                        settings.RightCharacter.PausedGifPath = "Assets/chika.gif";
                    }

                    settings.WidgetWidth = 720;
                    settings.WidgetHeight = 260;
                    CurrentSettings = settings;
                    SaveSettings(CurrentSettings);
                    return CurrentSettings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        CurrentSettings = new AppSettings();
        SaveSettings(CurrentSettings);
        return CurrentSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            CurrentSettings = settings;
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            SettingsChanged?.Invoke(CurrentSettings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(CurrentSettings);
        SaveSettings(CurrentSettings);
    }
}
