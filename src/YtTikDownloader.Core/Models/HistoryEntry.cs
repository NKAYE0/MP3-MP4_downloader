namespace YtTikDownloader.Core.Models;

/// <summary>
/// A single completed (or failed) download, persisted to history.json.
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
}
