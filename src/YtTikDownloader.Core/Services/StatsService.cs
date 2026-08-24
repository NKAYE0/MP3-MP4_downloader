using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Computes download statistics live from the history store. No separate
/// counters are persisted, so stats can never drift out of sync with the
/// history list they're derived from.
/// </summary>
public sealed class StatsService
{
    private readonly HistoryRepository _history;

    public StatsService(HistoryRepository history)
    {
        _history = history;
    }

    public StatsSnapshot ComputeSnapshot()
    {
        var entries = _history.GetAll();

        var byCategory = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var byFormat = entries
            .GroupBy(e => e.Format)
            .ToDictionary(g => g.Key, g => g.Count());

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-29));
        var byDay = entries
            .Select(e => DateOnly.FromDateTime(e.DownloadedAtUtc.UtcDateTime))
            .Where(d => d >= cutoff)
            .GroupBy(d => d)
            .ToDictionary(g => g.Key, g => g.Count());

        return new StatsSnapshot
        {
            TotalDownloads = entries.Count,
            SuccessfulDownloads = entries.Count(e => e.Success),
            FailedDownloads = entries.Count(e => !e.Success),
            TotalBytesDownloaded = entries.Sum(e => e.TotalFileSizeBytes),
            DownloadsByCategory = byCategory,
            DownloadsByFormat = byFormat,
            SponsorBlockSegmentsRemovedCount = entries.Count(e => e.SponsorBlockApplied),
            FirstDownloadAtUtc = entries.Count > 0 ? entries.Min(e => e.DownloadedAtUtc) : null,
            LastDownloadAtUtc = entries.Count > 0 ? entries.Max(e => e.DownloadedAtUtc) : null,
            DownloadsByDayLast30Days = byDay
        };
    }
}
