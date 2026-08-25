using System.Text.Json.Serialization;

namespace YtTikDownloader.Core.Models;

/// <summary>
/// A single completed (or failed) download, persisted to history.json. A
/// playlist/album download produces one HistoryEntry per track rather than
/// one entry for the whole batch, so each file shows up (and counts toward
/// Stats) individually; PlaylistGroupId ties those entries back together so
/// History can still show they were downloaded as one playlist/album.
/// </summary>
public sealed class HistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required MediaCategory Category { get; init; }
    public required DownloadFormat Format { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> FilePaths { get; init; } = new();
    public string? ThumbnailPath { get; init; }
    public long TotalFileSizeBytes { get; init; }
    public DateTimeOffset DownloadedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool SponsorBlockApplied { get; init; }

    /// <summary>
    /// Shared by every track that came from the same playlist/album
    /// download; null for a standalone (non-playlist) download. Only set
    /// when more than one file actually resulted from that download, so a
    /// playlist URL that only grabbed a single item doesn't get tagged as
    /// a "batch" of one.
    /// </summary>
    public string? PlaylistGroupId { get; init; }
    public string? PlaylistTitle { get; init; }
    public int? PlaylistIndex { get; init; }
    public int? PlaylistTotalCount { get; init; }

    /// <summary>How History displays the playlist/album grouping. Not persisted -- purely a UI convenience.</summary>
    [JsonIgnore]
    public string PlaylistLabel => PlaylistGroupId is null
        ? string.Empty
        : PlaylistIndex is not null && PlaylistTotalCount is not null
            ? $"{PlaylistTitle} ({PlaylistIndex}/{PlaylistTotalCount})"
            : PlaylistTitle ?? string.Empty;
}
