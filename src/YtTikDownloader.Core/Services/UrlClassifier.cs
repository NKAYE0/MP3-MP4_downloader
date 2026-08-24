using System.Text.RegularExpressions;
using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Figures out which tab (YouTube / TikTok / YouTube Music) a URL belongs
/// to, and whether it's a single item or a playlist/album, purely by
/// pattern-matching the URL text. Used for pasted text, drag-and-drop, and
/// clipboard detection.
/// </summary>
public static class UrlClassifier
{
    private static readonly Regex YouTubeMusicHost = new(
        @"^https?://music\.youtube\.com/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubeHost = new(
        @"^https?://(www\.|m\.)?(youtube\.com|youtu\.be)/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokHost = new(
        @"^https?://(www\.|vm\.|vt\.|m\.)?tiktok\.com/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HasPlaylistParam = new(
        @"[?&]list=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IsPlaylistPath = new(
        @"/playlist\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IsBrowseAlbumPath = new(
        @"/browse/(MPRE|OLAK5uy)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Classifies a single URL string (already trimmed of surrounding
    /// whitespace by the caller is not required, this trims itself).
    /// </summary>
    public static ClassifiedUrl Classify(string? url)
    {
        url = url?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return ClassifiedUrl.Unsupported(url);

        if (YouTubeMusicHost.IsMatch(url))
        {
            return new ClassifiedUrl
            {
                OriginalUrl = url,
                IsSupported = true,
                Category = MediaCategory.YouTubeMusic,
                Kind = DetermineYouTubeMusicKind(url)
            };
        }

        if (YouTubeHost.IsMatch(url))
        {
            return new ClassifiedUrl
            {
                OriginalUrl = url,
                IsSupported = true,
                Category = MediaCategory.YouTube,
                Kind = DetermineYouTubeKind(url)
            };
        }

        if (TikTokHost.IsMatch(url))
        {
            return new ClassifiedUrl
            {
                OriginalUrl = url,
                IsSupported = true,
                Category = MediaCategory.TikTok,
                Kind = UrlKind.SingleVideo
            };
        }

        return ClassifiedUrl.Unsupported(url);
    }

    /// <summary>Splits a block of pasted/dropped text into individual candidate URLs.</summary>
    public static IEnumerable<string> ExtractUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var matches = Regex.Matches(text, @"https?://\S+", RegexOptions.IgnoreCase);
        foreach (Match m in matches)
        {
            // Trim common trailing punctuation that tends to stick to URLs
            // when pasted from prose (e.g. "check this out: <url>.").
            yield return m.Value.TrimEnd('.', ',', ')', ']', '>', '"', '\'');
        }
    }

    private static UrlKind DetermineYouTubeKind(string url)
    {
        if (IsPlaylistPath.IsMatch(url)) return UrlKind.Playlist;
        if (HasPlaylistParam.IsMatch(url)) return UrlKind.Playlist;
        return UrlKind.SingleVideo;
    }

    private static UrlKind DetermineYouTubeMusicKind(string url)
    {
        if (IsBrowseAlbumPath.IsMatch(url)) return UrlKind.Album;
        if (IsPlaylistPath.IsMatch(url)) return UrlKind.Playlist;
        if (HasPlaylistParam.IsMatch(url)) return UrlKind.Playlist;
        return UrlKind.SingleVideo;
    }
}
