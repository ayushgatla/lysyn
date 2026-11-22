using System;
using System.Text;
using LyricIsland.Core.Models;
using LyricIsland.Core.Native;

namespace LyricIsland.Core.Services;

public enum MediaSourceType
{
    Spotify,
    AppleMusic,
    YouTubeMusic,
    YouTube,
    Generic
}

public readonly record struct DetectedSource(MediaSourceType Type, string DisplayName);

public static class MediaSourceDetector
{
    public static DetectedSource Detect(MediaTrackInfo track)
    {
        string app = track.SourceApp ?? string.Empty;
        string title = track.Title ?? string.Empty;
        string artist = track.Artist ?? string.Empty;
        string album = track.Album ?? string.Empty;

        // 1. Spotify
        if (app.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            return new(MediaSourceType.Spotify, "Spotify");
        }

        // 2. Apple Music / iTunes
        if (app.Contains("Apple", StringComparison.OrdinalIgnoreCase) ||
            app.Contains("iTunes", StringComparison.OrdinalIgnoreCase))
        {
            return new(MediaSourceType.AppleMusic, "Apple Music");
        }

        // 3. YouTube Music (Explicit PWAs, desktop apps, or explicit tags)
        if (app.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) ||
            app.Contains("YouTubeMusic", StringComparison.OrdinalIgnoreCase) ||
            app.Contains("cinhimbnkkaeohfgghhklpknlkffjgod", StringComparison.OrdinalIgnoreCase) || // Chrome/Edge YouTube Music PWA
            app.Contains("th-ch", StringComparison.OrdinalIgnoreCase) ||
            app.Contains("ytmdesktop", StringComparison.OrdinalIgnoreCase) ||
            album.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) ||
            title.EndsWith("YouTube Music", StringComparison.OrdinalIgnoreCase))
        {
            return new(MediaSourceType.YouTubeMusic, "YouTube Music");
        }

        // 4. YouTube PWA ID or explicit YouTube tags
        if (app.Contains("agimnkijcaahngcdmfeangaknmldooml", StringComparison.OrdinalIgnoreCase) || // Chrome/Edge YouTube PWA
            album.Equals("YouTube", StringComparison.OrdinalIgnoreCase) ||
            artist.Equals("YouTube", StringComparison.OrdinalIgnoreCase))
        {
            return new(MediaSourceType.YouTube, "YouTube");
        }

        // 5. Browser check (Zen Browser, Chrome, Edge, Firefox, Brave, Opera, Vivaldi, Arc, Floorp, Waterfox)
        bool isBrowser = IsBrowserApp(app);

        // YouTube video indicators (VEVO channels, - Topic, Official Video/Audio, Lyric Video, etc.)
        bool hasYouTubeClues =
            artist.EndsWith("VEVO", StringComparison.OrdinalIgnoreCase) ||
            artist.Contains("VEVO", StringComparison.OrdinalIgnoreCase) ||
            artist.EndsWith("- Topic", StringComparison.OrdinalIgnoreCase) ||
            artist.Contains("- Topic", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(Official Video)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("[Official Video]", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(Official Music Video)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("[Official Music Video]", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(Official Audio)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("[Official Audio]", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(Lyric Video)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("[Lyric Video]", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(Music Video)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("[Music Video]", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("YouTube", StringComparison.OrdinalIgnoreCase);

        if (hasYouTubeClues)
        {
            // If an album is provided that isn't "YouTube", it's likely YouTube Music
            if (!string.IsNullOrWhiteSpace(album) && !album.Equals("YouTube", StringComparison.OrdinalIgnoreCase))
            {
                return new(MediaSourceType.YouTubeMusic, "YouTube Music");
            }
            return new(MediaSourceType.YouTube, "YouTube");
        }

        if (isBrowser)
        {
            // Check top-level window titles for active YouTube or YouTube Music tab
            var windowClue = CheckBrowserWindowTitles();
            if (windowClue.HasValue)
            {
                return windowClue.Value;
            }

            // In YouTube Music, SMTC populates AlbumTitle; standard YouTube does not
            if (!string.IsNullOrWhiteSpace(album))
            {
                return new(MediaSourceType.YouTubeMusic, "YouTube Music");
            }

            // Default browser media session to YouTube
            return new(MediaSourceType.YouTube, "YouTube");
        }

        // Direct YouTube mention in app
        if (app.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
        {
            return new(MediaSourceType.YouTube, "YouTube");
        }

        return new(MediaSourceType.Generic, CleanAppDisplayName(app));
    }

    private static bool IsBrowserApp(string app)
    {
        if (string.IsNullOrWhiteSpace(app)) return false;
        return app.Contains("F0DC299D809B9700", StringComparison.OrdinalIgnoreCase) || // Zen Browser AUMID
               app.Contains("zen", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("msedge", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Firefox", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("308046B0AF4A39CB", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Brave", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Opera", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Floorp", StringComparison.OrdinalIgnoreCase) ||
               app.Contains("Waterfox", StringComparison.OrdinalIgnoreCase);
    }

    public static string CleanAppDisplayName(string app)
    {
        if (string.IsNullOrWhiteSpace(app)) return "Media Session";
        if (app.Contains("F0DC299D809B9700", StringComparison.OrdinalIgnoreCase) || app.Contains("zen", StringComparison.OrdinalIgnoreCase))
            return "Zen Browser";
        if (app.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) return "Chrome";
        if (app.Contains("msedge", StringComparison.OrdinalIgnoreCase) || app.Contains("Edge", StringComparison.OrdinalIgnoreCase)) return "Edge";
        if (app.Contains("Firefox", StringComparison.OrdinalIgnoreCase) || app.Contains("308046B0AF4A39CB", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (app.Contains("Brave", StringComparison.OrdinalIgnoreCase)) return "Brave";
        if (app.Contains("Opera", StringComparison.OrdinalIgnoreCase)) return "Opera";
        if (app.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase)) return "Vivaldi";
        if (app.Contains("Arc", StringComparison.OrdinalIgnoreCase)) return "Arc";
        if (app.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) return "Spotify";
        if (app.Contains("Apple", StringComparison.OrdinalIgnoreCase) || app.Contains("iTunes", StringComparison.OrdinalIgnoreCase)) return "Apple Music";

        // Clean up package or file names like "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify"
        int bangIndex = app.LastIndexOf('!');
        if (bangIndex >= 0 && bangIndex < app.Length - 1)
        {
            return app[(bangIndex + 1)..];
        }

        if (app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return app[..^4];
        }

        return app;
    }

    private static DetectedSource? CheckBrowserWindowTitles()
    {
        try
        {
            DetectedSource? found = null;
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hwnd)) return true;
                var sb = new StringBuilder(256);
                int len = NativeMethods.GetWindowText(hwnd, sb, 256);
                if (len <= 0) return true;

                string title = sb.ToString();
                if (title.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase))
                {
                    found = new(MediaSourceType.YouTubeMusic, "YouTube Music");
                    return false; // Stop enumeration
                }
                if (title.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
                {
                    found = new(MediaSourceType.YouTube, "YouTube");
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }
        catch
        {
            return null;
        }
    }
}
