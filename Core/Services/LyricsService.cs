using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LyricIsland.Core.Models;

namespace LyricIsland.Core.Services;

public partial class LyricsService : IDisposable
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "lysyn",
        "Cache"
    );

    private static readonly string LegacyCacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LyricIsland",
        "Cache"
    );

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static LyricsService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "lysyn/1.0 (Windows; DynamicLyricsWidget)");
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
            // Migrate cached lyrics from legacy folder if present
            if (Directory.Exists(LegacyCacheDirectory))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(LegacyCacheDirectory, "*.json"))
                    {
                        var dest = Path.Combine(CacheDirectory, Path.GetFileName(file));
                        if (!File.Exists(dest)) File.Copy(file, dest, true);
                    }
                }
                catch { }
            }
        }
    }

    public async Task<LyricData?> GetLyricsAsync(MediaTrackInfo track)
    {
        if (track.IsEmpty) return null;

        var (cleanTitle, cleanArtist) = NormalizeTrackAndArtist(track.Title, track.Artist);
        if (string.IsNullOrWhiteSpace(cleanTitle)) return null;

        // Check local disk cache first
        var cacheKey = ComputeCacheKey(cleanTitle, cleanArtist);
        var cached = LoadFromCache(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            // 1. Try exact match from LRCLIB
            var exactUrl = BuildExactUrl(cleanTitle, cleanArtist, track.Album, track.Duration.TotalSeconds);
            var response = await HttpClient.GetAsync(exactUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = ParseLrclibJson(content);
                if (data != null && (data.HasSyncedLyrics || !string.IsNullOrWhiteSpace(data.PlainLyrics)))
                {
                    SaveToCache(cacheKey, data);
                    return data;
                }
            }

            // 2. Try search endpoint with track_name and artist_name
            if (!string.IsNullOrWhiteSpace(cleanArtist))
            {
                var searchUrl = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(cleanTitle)}&artist_name={Uri.EscapeDataString(cleanArtist)}";
                var searchResult = await QuerySearchEndpointAsync(searchUrl, cleanArtist);
                if (searchResult != null)
                {
                    SaveToCache(cacheKey, searchResult);
                    return searchResult;
                }
            }

            // 3. Fallback: Search with query "artist title" (q=...)
            if (!string.IsNullOrWhiteSpace(cleanArtist))
            {
                var qUrl = $"https://lrclib.net/api/search?q={Uri.EscapeDataString($"{cleanArtist} {cleanTitle}")}";
                var qResult = await QuerySearchEndpointAsync(qUrl, cleanArtist);
                if (qResult != null)
                {
                    SaveToCache(cacheKey, qResult);
                    return qResult;
                }
            }

            // 4. Fallback: Search endpoint with track_name only
            var titleOnlyUrl = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(cleanTitle)}";
            var titleResult = await QuerySearchEndpointAsync(titleOnlyUrl, cleanArtist);
            if (titleResult != null)
            {
                SaveToCache(cacheKey, titleResult);
                return titleResult;
            }

            // 5. Aggressive clean fallback (strip any remaining bracketed text)
            var minimalTitle = Regex.Replace(cleanTitle, @"\s*[\(\[\{].*?[\)\]\}]", "").Trim();
            if (minimalTitle.Length > 2 && !string.Equals(minimalTitle, cleanTitle, StringComparison.OrdinalIgnoreCase))
            {
                var minimalUrl = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(minimalTitle)}" +
                    (!string.IsNullOrWhiteSpace(cleanArtist) ? $"&artist_name={Uri.EscapeDataString(cleanArtist)}" : "");
                var minResult = await QuerySearchEndpointAsync(minimalUrl, cleanArtist);
                if (minResult != null)
                {
                    SaveToCache(cacheKey, minResult);
                    return minResult;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to retrieve lyrics from LRCLIB: {ex.Message}");
        }

        return null;
    }

    private static async Task<LyricData?> QuerySearchEndpointAsync(string searchUrl, string? preferredArtist = null)
    {
        try
        {
            var searchResponse = await HttpClient.GetAsync(searchUrl);
            if (!searchResponse.IsSuccessStatusCode) return null;

            var content = await searchResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return null;

            JsonElement? bestElement = null;

            // First pass: look for an item matching preferred artist with syncedLyrics
            if (!string.IsNullOrWhiteSpace(preferredArtist))
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    string artistName = item.TryGetProperty("artistName", out var an) ? an.GetString() ?? "" : "";
                    bool isArtistMatch = artistName.Contains(preferredArtist, StringComparison.OrdinalIgnoreCase) ||
                                         preferredArtist.Contains(artistName, StringComparison.OrdinalIgnoreCase);

                    if (isArtistMatch && HasSyncedLyrics(item))
                    {
                        bestElement = item;
                        break;
                    }
                }
            }

            // Second pass: any item with syncedLyrics
            if (bestElement == null)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (HasSyncedLyrics(item))
                    {
                        bestElement = item;
                        break;
                    }
                }
            }

            // Third pass: first item in results
            if (bestElement == null && doc.RootElement.GetArrayLength() > 0)
            {
                bestElement = doc.RootElement[0];
            }

            if (bestElement != null)
            {
                return ParseLrclibJson(bestElement.Value.GetRawText());
            }
        }
        catch { }

        return null;
    }

    private static bool HasSyncedLyrics(JsonElement item)
    {
        return item.TryGetProperty("syncedLyrics", out var syncedProp) &&
               syncedProp.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(syncedProp.GetString());
    }

    private static string BuildExactUrl(string title, string artist, string album, double durationSeconds)
    {
        var sb = new StringBuilder("https://lrclib.net/api/get?");
        sb.Append($"track_name={Uri.EscapeDataString(title)}");
        sb.Append($"&artist_name={Uri.EscapeDataString(artist)}");

        if (!string.IsNullOrWhiteSpace(album))
        {
            sb.Append($"&album_name={Uri.EscapeDataString(album)}");
        }

        if (durationSeconds > 0)
        {
            sb.Append($"&duration={Math.Round(durationSeconds)}");
        }

        return sb.ToString();
    }

    private static LyricData? ParseLrclibJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = new LyricData
            {
                TrackName = root.TryGetProperty("trackName", out var tn) ? tn.GetString() ?? "" : "",
                ArtistName = root.TryGetProperty("artistName", out var an) ? an.GetString() ?? "" : "",
                AlbumName = root.TryGetProperty("albumName", out var aln) ? aln.GetString() ?? "" : "",
                Duration = root.TryGetProperty("duration", out var dur) ? dur.GetDouble() : 0,
                Instrumental = root.TryGetProperty("instrumental", out var inst) && inst.GetBoolean(),
                PlainLyrics = root.TryGetProperty("plainLyrics", out var pl) ? pl.GetString() : null,
                SyncedLyrics = root.TryGetProperty("syncedLyrics", out var sl) ? sl.GetString() : null,
            };

            if (!string.IsNullOrWhiteSpace(data.SyncedLyrics))
            {
                data.ParsedLines = ParseLrcString(data.SyncedLyrics);
            }

            return data;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"^\[(\d{2,}):(\d{2}(?:\.\d{1,3})?)\](.*)$")]
    private static partial Regex LrcLineRegex();

    public static List<LyricLine> ParseLrcString(string lrc)
    {
        var result = new List<LyricLine>();
        var lines = lrc.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var regex = LrcLineRegex();

        foreach (var line in lines)
        {
            var match = regex.Match(line.Trim());
            if (match.Success)
            {
                var minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var seconds = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var text = match.Groups[3].Value.Trim();

                var ts = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                result.Add(new LyricLine(ts, text));
            }
        }

        result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return result;
    }

    public static (string CleanTitle, string CleanArtist) NormalizeTrackAndArtist(string rawTitle, string rawArtist)
    {
        if (string.IsNullOrWhiteSpace(rawTitle)) return (string.Empty, CleanArtist(rawArtist));

        string titleCandidate = rawTitle.Trim();
        string artistCandidate = (rawArtist ?? string.Empty).Trim();

        // 1. Detect "Artist - Title" format (standard in YouTube music videos)
        var dashMatch = Regex.Match(titleCandidate, @"\s+(?:[-–—]|:\s)\s+");
        if (dashMatch.Success)
        {
            var leftPart = titleCandidate[..dashMatch.Index].Trim();
            var rightPart = titleCandidate[(dashMatch.Index + dashMatch.Length)..].Trim();

            // If leftPart is of reasonable artist length (<= 45 chars) and rightPart is non-empty
            if (leftPart.Length > 0 && leftPart.Length <= 45 && rightPart.Length > 0)
            {
                artistCandidate = leftPart;
                titleCandidate = rightPart;
            }
        }

        // 2. Clean the artist
        artistCandidate = CleanArtist(artistCandidate);

        // 3. Clean the title
        titleCandidate = CleanTrackTitle(titleCandidate);

        return (titleCandidate, artistCandidate);
    }

    public static string CleanTrackTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        string cleaned = title.Trim();

        // 1. Remove wrapping quotes
        cleaned = cleaned.Trim('"', '\'', '“', '”', '‘', '’');

        // 2. Strip trailing pipe / slash metadata like " | Official Music Video" or " // Lyrics"
        cleaned = Regex.Replace(cleaned, @"\s*[\|/]{1,2}\s*.*$", "");

        // 3. Remove parenthesized/bracketed chunks containing noise keywords
        // (lyrics, lyric, live, extended, official, video, audio, visualizer, remaster, deluxe, version, edit, mix, 4k, hd, hq, feat, ft, clean, explicit, full, etc.)
        string bracketPattern = @"\s*[\(\[\{][^\(\)\[\]\{\}]*?(?:lyrics?|live|extended|official|video|audio|visualizer|remaster(?:ed)?|deluxe|version|edit|mix|4k|hd|hq|feat\.?|ft\.?|clean|explicit|full)[^\(\)\[\]\{\}]*?[\)\]\}]";
        cleaned = Regex.Replace(cleaned, bracketPattern, "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, bracketPattern, "", RegexOptions.IgnoreCase); // Repeat for consecutive brackets

        // 4. Strip trailing unparenthesized noise (e.g. " - Live at Wembley", " - Remastered 2011", " - Extended Mix", " Lyrics", " Official Video")
        cleaned = Regex.Replace(cleaned, @"\s*-\s*(?:live|extended|remaster(?:ed)?|deluxe|mix|version|audio|video|edit).*", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+(?:lyrics?|official\s*video|official\s*audio|official\s*music\s*video|live|extended)\s*$", "", RegexOptions.IgnoreCase);

        // 5. Strip trailing feat / ft (e.g. "ft. Nate Ruess", "feat. Drake")
        cleaned = Regex.Replace(cleaned, @"\s*(?:feat\.|ft\.|featuring)\s+.*$", "", RegexOptions.IgnoreCase);

        // 6. Strip empty brackets
        cleaned = Regex.Replace(cleaned, @"\s*[\(\[\{]\s*[\)\]\}]", "");

        // Clean up any remaining multiple spaces or trailing punctuation
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
        cleaned = cleaned.Trim(' ', '-', '–', '—', ':', ',', '.');

        return cleaned;
    }

    public static string CleanArtist(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist)) return string.Empty;

        string cleaned = artist.Trim();

        // Strip " - Topic" (YouTube auto-generated channels, e.g. "Queen - Topic" -> "Queen")
        cleaned = Regex.Replace(cleaned, @"(?i)\s*-\s*topic$", "");

        // Strip "VEVO" (e.g. WhamVEVO -> Wham, PinkVEVO -> Pink)
        cleaned = Regex.Replace(cleaned, @"(?i)vevo$", "");

        // Strip "Official", "Channel", "Records", "Music" at the end if it's a channel name
        cleaned = Regex.Replace(cleaned, @"(?i)\s*(official\s*(channel|music)?|channel|records)$", "");

        // If multiple artists separated by commas or semicolons, take primary artist
        var idx = cleaned.IndexOfAny(new[] { ',', ';' });
        if (idx > 0) cleaned = cleaned[..idx];

        // Strip feat. or ft. in artist
        cleaned = Regex.Replace(cleaned, @"(?i)\s*(?:feat\.|ft\.|featuring)\s+.*$", "");

        return cleaned.Trim();
    }

    private static string ComputeCacheKey(string title, string artist)
    {
        var raw = $"{title.ToLowerInvariant()}|{artist.ToLowerInvariant()}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static LyricData? LoadFromCache(string key)
    {
        try
        {
            var path = Path.Combine(CacheDirectory, $"{key}.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<LyricData>(json, JsonOpts);
                if (data != null && !string.IsNullOrWhiteSpace(data.SyncedLyrics) && data.ParsedLines.Count == 0)
                {
                    data.ParsedLines = ParseLrcString(data.SyncedLyrics);
                }
                return data;
            }
        }
        catch { }
        return null;
    }

    private static void SaveToCache(string key, LyricData data)
    {
        try
        {
            var path = Path.Combine(CacheDirectory, $"{key}.json");
            var json = JsonSerializer.Serialize(data, JsonOpts);
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public void Dispose()
    {
        // Static HttpClient persists across lifecycle
    }
}
