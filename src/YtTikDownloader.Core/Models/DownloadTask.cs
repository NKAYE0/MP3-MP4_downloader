using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YtTikDownloader.Core.Models;

/// <summary>
/// One queue entry: wraps a <see cref="DownloadRequest"/> with live progress
/// state. Implements INotifyPropertyChanged directly (no MVVM toolkit
/// dependency) so WPF can bind to it straight from the queue collection.
/// </summary>
public sealed class DownloadTask : INotifyPropertyChanged
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public required DownloadRequest Request { get; init; }

    private DownloadStatus _status = DownloadStatus.Queued;
    public DownloadStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(HasResultFiles));
        }
    }

    /// <summary>
    /// True once the task has finished successfully and has at least one
    /// output file. ResultFilePaths is a plain List (not observable), so
    /// this is recomputed and re-announced whenever Status changes rather
    /// than relying on the list itself to notify bindings.
    /// </summary>
    public bool HasResultFiles => Status == DownloadStatus.Completed && ResultFilePaths.Count > 0;

    private double _progressPercent;
    public double ProgressPercent
    {
        get => _progressPercent;
        set { _progressPercent = value; OnPropertyChanged(); }
    }

    private string? _speedText;
    public string? SpeedText
    {
        get => _speedText;
        set { _speedText = value; OnPropertyChanged(); }
    }

    private string? _etaText;
    public string? EtaText
    {
        get => _etaText;
        set { _etaText = value; OnPropertyChanged(); }
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private string? _currentItemLabel;
    /// <summary>e.g. "Track 3 of 12" while a playlist/album is in progress.</summary>
    public string? CurrentItemLabel
    {
        get => _currentItemLabel;
        set { _currentItemLabel = value; OnPropertyChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public List<string> ResultFilePaths { get; } = new();
    public List<string> ResultThumbnailPaths { get; } = new();

    public bool IsActive => Status is DownloadStatus.Queued or DownloadStatus.FetchingInfo
        or DownloadStatus.Downloading or DownloadStatus.PostProcessing;

    public CancellationTokenSource CancellationSource { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
