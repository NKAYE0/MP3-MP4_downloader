namespace YtTikDownloader.Core.Models;

/// <summary>
/// Everything needed to kick off one yt-dlp invocation. Built by the UI
/// from the tab the user is on plus whatever options they've chosen, then
/// handed to the download engine.
/// </summary>
public sealed class DownloadRequest
{
    public required string Url { get; init; }
    public required MediaCategory Category { get; init; }
    public required UrlKind Kind { get; init; }
    public required DownloadFormat Format { get; init; }
    public required string OutputFolder { get; init; }

    /// <summary>Save a standalone thumbnail/cover-art image file next to the media.</summary>
    public bool WriteThumbnail { get; init; }

    /// <summary>Embed the thumbnail/cover art inside the media file itself.</summary>
    public bool EmbedThumbnail { get; init; } = true;

    /// <summary>Embed title/artist/album etc. metadata tags into the file.</summary>
    public bool EmbedMetadata { get; init; } = true;

    /// <summary>SponsorBlock categories to cut out of the downloaded video. Empty = disabled.</summary>
    public IReadOnlyList<SponsorBlockCategory> SponsorBlockRemoveCategories { get; init; } = Array.Empty<SponsorBlockCategory>();

    /// <summary>Optional "start-end" playlist item range, e.g. "1-10". Null = all items.</summary>
    public string? PlaylistItems { get; init; }

    /// <summary>For a playlist/album URL, whether to fetch every item or just the single video/track the link points at.</summary>
    public bool DownloadEntirePlaylist { get; init; } = true;
}
