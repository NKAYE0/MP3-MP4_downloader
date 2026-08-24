namespace YtTikDownloader.Core.Models;

public enum DownloadStatus
{
    Queued,
    FetchingInfo,
    Downloading,
    PostProcessing,
    Completed,
    Failed,
    Canceled
}
