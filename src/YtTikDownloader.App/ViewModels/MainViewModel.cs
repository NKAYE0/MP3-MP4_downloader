using System.Linq;
using System.Windows.Input;
using YtTikDownloader.App.Services;
using YtTikDownloader.Core.Models;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.ViewModels;

/// <summary>
/// Application-level view model. Owns every Core service, the three
/// category tab view models, and the small amount of cross-cutting state
/// (status bar text, the pending-clipboard-URL banner, and routing a
/// "play this file" request out to whoever hosts the player).
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public SettingsService Settings { get; }
    public HistoryRepository History { get; }
    public StatsService Stats { get; }
    public YtDlpBinaryManager BinaryManager { get; }
    public DownloadQueueManager QueueManager { get; }
    public DownloadOptionsPresetRepository PresetRepository { get; }

    public CategoryTabViewModel YouTubeTab { get; }
    public CategoryTabViewModel TikTokTab { get; }
    public CategoryTabViewModel YouTubeMusicTab { get; }

    public HistoryViewModel HistoryVm { get; }
    public StatsViewModel StatsVm { get; }
    public SettingsViewModel SettingsVm { get; }

    /// <summary>Raised when a "Play" button is clicked; the view subscribes and hands the path to the player.</summary>
    public event Action<string>? PlayRequested;

    private string _statusMessage = "Ready.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private string? _pendingClipboardUrl;
    public string? PendingClipboardUrl
    {
        get => _pendingClipboardUrl;
        set => SetField(ref _pendingClipboardUrl, value);
    }

    private ClassifiedUrl? _pendingClipboardClassified;

    public ICommand AddPendingClipboardUrlCommand { get; }
    public ICommand DismissPendingClipboardUrlCommand { get; }

    public MainViewModel()
    {
        AppPaths.EnsureCoreFoldersExist();

        Settings = new SettingsService();
        AccentColorService.Apply(Settings.Current.AccentColorHex);

        History = new HistoryRepository();
        Stats = new StatsService(History);
        PresetRepository = new DownloadOptionsPresetRepository();
        BinaryManager = new YtDlpBinaryManager(Settings);
        var engine = new YtDlpDownloadEngine(BinaryManager);
        QueueManager = new DownloadQueueManager(engine, History, Settings);

        HistoryVm = new HistoryViewModel(History);
        StatsVm = new StatsViewModel(Stats);
        SettingsVm = new SettingsViewModel(Settings, BinaryManager, QueueManager);

        // Subscribed after HistoryVm/StatsVm exist, since the handler
        // references both (only ever invoked later, once a download
        // finishes, but declaring it before they're assigned would leave
        // the compiler unable to prove they're non-null at that point).
        QueueManager.TaskFinished += (_, entries) =>
        {
            StatusMessage = BuildFinishedStatusMessage(entries);
            HistoryVm.Refresh();
            StatsVm.Refresh();
        };

        YouTubeTab = new CategoryTabViewModel(MediaCategory.YouTube, "YouTube", this);
        TikTokTab = new CategoryTabViewModel(MediaCategory.TikTok, "TikTok", this);
        YouTubeMusicTab = new CategoryTabViewModel(MediaCategory.YouTubeMusic, "YouTube Music", this);

        AddPendingClipboardUrlCommand = new RelayCommand(_ =>
        {
            if (_pendingClipboardClassified is { } classified) RouteClassified(classified);
            PendingClipboardUrl = null;
            _pendingClipboardClassified = null;
        });
        DismissPendingClipboardUrlCommand = new RelayCommand(_ =>
        {
            PendingClipboardUrl = null;
            _pendingClipboardClassified = null;
        });
    }

    /// <summary>
    /// A playlist/album download now finishes with one HistoryEntry per
    /// track rather than one entry for the whole batch, so the status bar
    /// message has to summarize the group instead of just naming a single
    /// entry.
    /// </summary>
    private static string BuildFinishedStatusMessage(IReadOnlyList<HistoryEntry> entries)
    {
        if (entries.Count == 0) return "Finished.";

        if (entries.Count == 1)
        {
            var entry = entries[0];
            return entry.Success
                ? $"Finished: {entry.Title}"
                : $"Failed: {entry.Title} — {entry.ErrorMessage}";
        }

        var succeeded = entries.Count(e => e.Success);
        var groupTitle = entries[0].PlaylistTitle ?? entries[0].Title;
        return $"Finished: {groupTitle} ({succeeded}/{entries.Count} tracks)";
    }

    public void RequestPlay(string filePath) => PlayRequested?.Invoke(filePath);

    /// <summary>Classifies each raw URL and hands it to the matching category tab. Used by manual add, drag-drop, and auto-queued clipboard URLs.</summary>
    public void RouteUrls(IEnumerable<string> rawUrls)
    {
        var added = 0;
        var skipped = 0;

        foreach (var raw in rawUrls)
        {
            var classified = UrlClassifier.Classify(raw);
            if (!classified.IsSupported)
            {
                skipped++;
                continue;
            }
            RouteClassified(classified);
            added++;
        }

        StatusMessage = (added, skipped) switch
        {
            (0, 0) => StatusMessage,
            (var a, 0) when a > 0 => $"Added {a} item(s) to the queue.",
            (0, var s) when s > 0 => $"{s} URL(s) weren't recognized as YouTube, YouTube Music, or TikTok links.",
            var (a, s) => $"Added {a} item(s); skipped {s} unrecognized URL(s)."
        };
    }

    private void RouteClassified(ClassifiedUrl classified)
    {
        var tab = classified.Category switch
        {
            MediaCategory.YouTube => YouTubeTab,
            MediaCategory.TikTok => TikTokTab,
            MediaCategory.YouTubeMusic => YouTubeMusicTab,
            _ => null
        };
        tab?.EnqueueClassified(classified);
    }

    /// <summary>Called by the clipboard monitor whenever the clipboard text changes.</summary>
    public void HandleClipboardTextDetected(string text)
    {
        var classified = UrlClassifier.Classify(text);
        if (!classified.IsSupported) return;

        if (Settings.Current.AutoQueueClipboardUrls)
        {
            RouteClassified(classified);
            StatusMessage = $"Auto-added from clipboard: {classified.OriginalUrl}";
        }
        else
        {
            _pendingClipboardClassified = classified;
            PendingClipboardUrl = classified.OriginalUrl;
        }
    }
}
