namespace YtTikDownloader.Core.Models;

/// <summary>
/// Aggregate download statistics, computed on demand from the history
/// store rather than kept as a separate persisted total (single source of
/// truth, and it can't drift out of sync with history).
/// </summary>
public sealed class StatsSnapshot
{
    public int TotalDownloads { get; init; }
    public int SuccessfulDownloads { get; init; }
    public int FailedDownloads { get; init; }
    public long TotalBytesDownloaded { get; init; }
    public Dictionary<MediaCategory, int> DownloadsByCategory { get; init; } = new();
    public Dictionary<DownloadFormat, int> DownloadsByFormat { get; init; } = new();
    public int SponsorBlockSegmentsRemovedCount { get; init; }
    public DateTimeOffset? FirstDownloadAtUtc { get; init; }
    public DateTimeOffset? LastDownloadAtUtc { get; init; }
    public Dictionary<DateOnly, int> DownloadsByDayLast30Days { get; init; } = new();
}
