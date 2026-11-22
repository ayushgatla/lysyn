using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LyricIsland.Assets;
using LyricIsland.Core.Models;
using LyricIsland.Core.Services;
using Xunit;

namespace LyricIsland.Tests;

public class CoreTests
{
    [Fact]
    public void Test_LrcParsing_ValidLrc_ParsedCorrectly()
    {
        string sampleLrc = @"
[00:05.20] First lyric line
[00:12.80] Second lyric line
[01:05.45] Third line after a minute
[01:30.00] Final outro line
";
        var lines = LyricsService.ParseLrcString(sampleLrc);

        Assert.Equal(4, lines.Count);
        Assert.Equal(TimeSpan.FromSeconds(5.20), lines[0].Timestamp);
        Assert.Equal("First lyric line", lines[0].Text);

        Assert.Equal(TimeSpan.FromSeconds(12.80), lines[1].Timestamp);
        Assert.Equal("Second lyric line", lines[1].Text);

        Assert.Equal(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(5.45), lines[2].Timestamp);
        Assert.Equal("Third line after a minute", lines[2].Text);
    }

    [Fact]
    public void Test_SettingsManager_SaveAndLoad()
    {
        var manager = new SettingsManager();
        var original = manager.CurrentSettings;

        double testWidth = 720.0;
        int testOffset = 350;

        manager.UpdateSettings(s =>
        {
            s.WidgetWidth = testWidth;
            s.TimingOffsetMs = testOffset;
        });

        // Re-read from disk
        var reloaded = manager.LoadSettings();

        Assert.Equal(testWidth, reloaded.WidgetWidth);
        Assert.Equal(testOffset, reloaded.TimingOffsetMs);
    }

    [Fact]
    public void Test_LyricsSynchronizer_ProgressionAndOffsets()
    {
        using var synchronizer = new LyricsSynchronizer();

        var lyricData = new LyricData
        {
            ParsedLines = new()
            {
                new LyricLine(TimeSpan.FromSeconds(10), "Line 1"),
                new LyricLine(TimeSpan.FromSeconds(20), "Line 2"),
                new LyricLine(TimeSpan.FromSeconds(30), "Line 3")
            }
        };

        synchronizer.SetLyrics(lyricData, TimeSpan.FromSeconds(60));

        LyricSyncEventArgs? lastArgs = null;
        synchronizer.Synchronized += (_, args) => lastArgs = args;

        // Simulate position at 15s (halfway through Line 1)
        synchronizer.UpdatePlaybackState(PlaybackState.Paused, TimeSpan.FromSeconds(15));

        Assert.NotNull(lastArgs);
        Assert.True(lastArgs.HasSyncedLyrics);
        Assert.Equal(0, lastArgs.ActiveIndex);
        Assert.Equal("Line 1", lastArgs.CurrentLine?.Text);
        Assert.Equal("Line 2", lastArgs.NextLine?.Text);
        Assert.Null(lastArgs.PreviousLine);
        Assert.True(lastArgs.LineProgressRatio >= 0.49 && lastArgs.LineProgressRatio <= 0.51);

        // Apply +10000ms offset (10s) -> effective position 25s (in Line 2)
        synchronizer.TimingOffsetMs = 10000;
        synchronizer.SyncTimelinePosition(TimeSpan.FromSeconds(15));

        Assert.NotNull(lastArgs);
        Assert.Equal(1, lastArgs.ActiveIndex);
        Assert.Equal("Line 2", lastArgs.CurrentLine?.Text);
        Assert.Equal("Line 1", lastArgs.PreviousLine?.Text);
        Assert.Equal("Line 3", lastArgs.NextLine?.Text);
    }

    [Fact]
    public async Task Test_LrclibApi_LiveFetchAndCache()
    {
        using var service = new LyricsService();

        var track = new MediaTrackInfo
        {
            Title = "Blinding Lights",
            Artist = "The Weeknd",
            Album = "After Hours",
            Duration = TimeSpan.FromSeconds(200)
        };

        var result = await service.GetLyricsAsync(track);

        Assert.NotNull(result);
        Assert.True(result.HasSyncedLyrics || !string.IsNullOrWhiteSpace(result.PlainLyrics));

        // Test second fetch hit cache immediately
        var cachedResult = await service.GetLyricsAsync(track);
        Assert.NotNull(cachedResult);
        Assert.Equal(result.TrackName, cachedResult.TrackName);
    }

    [Fact]
    public void Test_CharacterAssetGeneration_AndGifDecoding()
    {
        AssetGenerator.EnsureDefaultCharactersExist();

        var catIdle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Characters", "cat_idle.gif");
        var bunnyPlaying = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Characters", "bunny_playing.gif");

        Assert.True(File.Exists(catIdle), $"File not found: {catIdle}");
        Assert.True(File.Exists(bunnyPlaying), $"File not found: {bunnyPlaying}");

        // Verify decoder parses multiple frames
        var decoder = BitmapDecoder.Create(new Uri(catIdle), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        Assert.True(decoder.Frames.Count > 1, $"Expected animated GIF with multiple frames, got {decoder.Frames.Count}");
    }

    [Fact]
    public void Test_MediaSourceDetection_ZenBrowser_YouTube()
    {
        var track = new MediaTrackInfo
        {
            SourceApp = "F0DC299D809B9700",
            Artist = "PinkVEVO",
            Title = "P!nk - Just Give Me a Reason (Official Video) ft....",
            Album = ""
        };

        var detected = MediaSourceDetector.Detect(track);

        Assert.Equal(MediaSourceType.YouTube, detected.Type);
        Assert.Equal("YouTube", detected.DisplayName);
    }

    [Fact]
    public void Test_MediaSourceDetection_YouTubeMusic_Pwa()
    {
        var track = new MediaTrackInfo
        {
            SourceApp = "Chrome._crx_cinhimbnkkaeohfgghhklpknlkffjgod",
            Artist = "Dua Lipa",
            Title = "Levitating",
            Album = "Future Nostalgia"
        };

        var detected = MediaSourceDetector.Detect(track);

        Assert.Equal(MediaSourceType.YouTubeMusic, detected.Type);
        Assert.Equal("YouTube Music", detected.DisplayName);
    }

    [Fact]
    public void Test_MediaSourceDetection_Spotify()
    {
        var track = new MediaTrackInfo
        {
            SourceApp = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify",
            Artist = "Queen",
            Title = "Bohemian Rhapsody",
            Album = "A Night at the Opera"
        };

        var detected = MediaSourceDetector.Detect(track);

        Assert.Equal(MediaSourceType.Spotify, detected.Type);
        Assert.Equal("Spotify", detected.DisplayName);
    }

    [Fact]
    public void Test_ThemeColorHelper_HueConversion()
    {
        // 38° is Amber Gold
        var amber = ThemeColorHelper.GetAccentColor(38);
        Assert.True(amber.R > 220);
        Assert.True(amber.G > 140 && amber.G < 180);
        Assert.True(amber.B < 40);

        // 180° is Cyan
        var cyan = ThemeColorHelper.GetAccentColor(180);
        Assert.True(cyan.G > 200);
        Assert.True(cyan.B > 200);
        Assert.True(cyan.R < 50);

        // -1 is Solid White
        var white = ThemeColorHelper.GetAccentColor(-1);
        Assert.Equal(255, white.R);
        Assert.Equal(255, white.G);
        Assert.Equal(255, white.B);

        // Roundtrip Hex
        string hex = ThemeColorHelper.ToHex(amber);
        Assert.StartsWith("#", hex);
        var parsed = ThemeColorHelper.FromHex(hex, System.Windows.Media.Colors.Black);
        Assert.Equal(amber.R, parsed.R);
        Assert.Equal(amber.G, parsed.G);
        Assert.Equal(amber.B, parsed.B);
    }

    [Fact]
    public void Test_LyricsService_NormalizeTrackAndArtist_YouTubeCleaning()
    {
        // 1. Wham! with VEVO and Official Video
        var (t1, a1) = LyricsService.NormalizeTrackAndArtist("Wham! - Last Christmas (Official Video)", "WhamVEVO");
        Assert.Equal("Last Christmas", t1);
        Assert.Equal("Wham!", a1);

        // 2. Word "lyrics" in title
        var (t2, a2) = LyricsService.NormalizeTrackAndArtist("Alan Walker - Faded (Lyrics)", "Alan Walker");
        Assert.Equal("Faded", t2);
        Assert.Equal("Alan Walker", a2);

        // 3. Word "live" in title and Topic in artist
        var (t3, a3) = LyricsService.NormalizeTrackAndArtist("Queen - Bohemian Rhapsody (Live at Wembley)", "Queen - Topic");
        Assert.Equal("Bohemian Rhapsody", t3);
        Assert.Equal("Queen", a3);

        // 4. Word "extended" in title
        var (t4, a4) = LyricsService.NormalizeTrackAndArtist("Imagine Dragons - Believer (Extended Version)", "Imagine Dragons");
        Assert.Equal("Believer", t4);
        Assert.Equal("Imagine Dragons", a4);

        // 5. P!nk with ft. and VEVO
        var (t5, a5) = LyricsService.NormalizeTrackAndArtist("P!nk - Just Give Me a Reason (Official Video) ft. Nate Ruess", "PinkVEVO");
        Assert.Equal("Just Give Me a Reason", t5);
        Assert.Equal("P!nk", a5);
    }

    [Fact]
    public async Task Test_LyricsService_LiveFetch_YouTubeWham()
    {
        using var service = new LyricsService();

        var track = new MediaTrackInfo
        {
            Title = "Wham! - Last Christmas (Official Video)",
            Artist = "WhamVEVO",
            Duration = TimeSpan.FromSeconds(270)
        };

        var result = await service.GetLyricsAsync(track);

        Assert.NotNull(result);
        Assert.True(result.HasSyncedLyrics);
        Assert.Equal("Last Christmas", result.TrackName);
    }
}
