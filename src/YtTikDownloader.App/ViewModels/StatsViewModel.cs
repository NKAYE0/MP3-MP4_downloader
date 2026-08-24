using System.Windows.Input;
using YtTikDownloader.Core.Models;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.ViewModels;

public sealed class StatsViewModel : ViewModelBase
{
    private readonly StatsService _stats;

    public ICommand RefreshCommand { get; }

    private int _totalDownloads;
    public int TotalDownloads { get => _totalDownloads; private set => SetField(ref _totalDownloads, value); }

    private int _successfulDownloads;
    public int SuccessfulDownloads { get => _successfulDownloads; private set => SetField(ref _successfulDownloads, value); }

    private int _failedDownloads;
    public int FailedDownloads { get => _failedDownloads; private set => SetField(ref _failedDownloads, value); }

    private string _totalSizeText = "0 B";
    public string TotalSizeText { get => _totalSizeText; private set => SetField(ref _totalSizeText, value); }

    private int _youTubeCount;
    public int YouTubeCount { get => _youTubeCount; private set => SetField(ref _youTubeCount, value); }

    private int _tikTokCount;
    public int TikTokCount { get => _tikTokCount; private set => SetField(ref _tikTokCount, value); }

    private int _youTubeMusicCount;
    public int YouTubeMusicCount { get => _youTubeMusicCount; private set => SetField(ref _youTubeMusicCount, value); }

    private int _videoDownloads;
    public int VideoDownloads { get => _videoDownloads; private set => SetField(ref _videoDownloads, value); }

    private int _audioDownloads;
    public int AudioDownloads { get => _audioDownloads; private set => SetField(ref _audioDownloads, value); }

    private int _sponsorBlockAppliedCount;
    public int SponsorBlockAppliedCount { get => _sponsorBlockAppliedCount; private set => SetField(ref _sponsorBlockAppliedCount, value); }

    private int _last30DaysTotal;
    public int Last30DaysTotal { get => _last30DaysTotal; private set => SetField(ref _last30DaysTotal, value); }

    public StatsViewModel(StatsService stats)
    {
        _stats = stats;
        RefreshCommand = new RelayCommand(_ => Refresh());
        Refresh();
    }

    public void Refresh()
    {
        var snapshot = _stats.ComputeSnapshot();

        TotalDownloads = snapshot.TotalDownloads;
        SuccessfulDownloads = snapshot.SuccessfulDownloads;
        FailedDownloads = snapshot.FailedDownloads;
        TotalSizeText = FormatSize(snapshot.TotalBytesDownloaded);

        YouTubeCount = snapshot.DownloadsByCategory.GetValueOrDefault(MediaCategory.YouTube);
        TikTokCount = snapshot.DownloadsByCategory.GetValueOrDefault(MediaCategory.TikTok);
        YouTubeMusicCount = snapshot.DownloadsByCategory.GetValueOrDefault(MediaCategory.YouTubeMusic);

        VideoDownloads = snapshot.DownloadsByFormat.GetValueOrDefault(DownloadFormat.Mp4Video);
        AudioDownloads = snapshot.DownloadsByFormat.GetValueOrDefault(DownloadFormat.Mp3Audio);

        SponsorBlockAppliedCount = snapshot.SponsorBlockSegmentsRemovedCount;
        Last30DaysTotal = snapshot.DownloadsByDayLast30Days.Values.Sum();
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.##} {units[unitIndex]}";
    }
}
