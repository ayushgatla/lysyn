namespace LyricIsland.Core.Models;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public class MediaTrackInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string SourceApp { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public TimeSpan Position { get; set; } = TimeSpan.Zero;
    public PlaybackState State { get; set; } = PlaybackState.Stopped;
    public byte[]? ThumbnailBytes { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Artist);

    public override bool Equals(object? obj)
    {
        if (obj is not MediaTrackInfo other) return false;
        return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Artist, other.Artist, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Album, other.Album, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Title.ToLowerInvariant(),
            Artist.ToLowerInvariant(),
            Album.ToLowerInvariant());
    }
}
