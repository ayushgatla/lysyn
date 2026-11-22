namespace LyricIsland.Core.Models;

public class LyricData
{
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public double Duration { get; set; }
    public bool Instrumental { get; set; }
    public string? PlainLyrics { get; set; }
    public string? SyncedLyrics { get; set; }
    public List<LyricLine> ParsedLines { get; set; } = new();

    public bool HasSyncedLyrics => ParsedLines.Count > 0;
}
