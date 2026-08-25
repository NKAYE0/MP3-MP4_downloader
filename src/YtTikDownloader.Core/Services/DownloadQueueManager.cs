using System.Collections.ObjectModel;
using System.Threading;
using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Owns the live download queue: an ObservableCollection the UI binds to
/// directly, plus a semaphore that limits how many yt-dlp processes run
/// at once. Every completed (or failed) task is written to history
/// automatically, so callers only need to call Enqueue and listen to
/// TaskFinished if they want to react (e.g. refresh the Stats tab).
/// </summary>
public sealed class DownloadQueueManager
{
    private readonly YtDlpDownloadEngine _engine;
    private readonly HistoryRepository _history;
    private readonly SettingsService _settings;
    private SemaphoreSlim _concurrencyGate;

    public ObservableCollection<DownloadTask> Queue { get; } = new();

    public event Action<DownloadTask, IReadOnlyList<HistoryEntry>>? TaskFinished;

    public DownloadQueueManager(YtDlpDownloadEngine engine, HistoryRepository history, SettingsService settings)
    {
        _engine = engine;
        _history = history;
        _settings = settings;
        _concurrencyGate = new SemaphoreSlim(Math.Max(1, settings.Current.MaxConcurrentDownloads));
    }

    /// <summary>Call after the user changes the concurrency setting.</summary>
    public void UpdateConcurrency(int maxConcurrent)
    {
        _concurrencyGate = new SemaphoreSlim(Math.Max(1, maxConcurrent));
    }

    public DownloadTask Enqueue(DownloadRequest request)
    {
        var task = new DownloadTask { Request = request, Title = request.Url };
        Queue.Insert(0, task);

        // Captured here, on whatever thread called Enqueue -- normally the
        // UI thread, since this is invoked directly from a button command.
        // yt-dlp's progress updates arrive on background threads later, and
        // this context is what lets them be marshaled back to the UI
        // thread safely (see RunTaskAsync / YtDlpDownloadEngine).
        var uiContext = SynchronizationContext.Current;
        _ = RunTaskAsync(task, uiContext);
        return task;
    }

    public void Cancel(DownloadTask task)
    {
        if (!task.CancellationSource.IsCancellationRequested)
            task.CancellationSource.Cancel();
    }

    public void RemoveFromQueue(DownloadTask task)
    {
        if (task.IsActive) Cancel(task);
        Queue.Remove(task);
    }

    private async Task RunTaskAsync(DownloadTask task, SynchronizationContext? uiContext)
    {
        // Capture the current gate instance up front: if the user changes
        // the concurrency setting (which swaps in a brand new
        // SemaphoreSlim) while this task is queued or running, we must
        // still Release() the exact same instance we Wait()ed on, not
        // whatever _concurrencyGate happens to point to by then.
        var gate = _concurrencyGate;

        try
        {
            await gate.WaitAsync(task.CancellationSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            SetStatus(uiContext, task, DownloadStatus.Canceled);
            return;
        }

        try
        {
            var entries = await _engine.RunAsync(
                task,
                uiContext,
                _settings.Current.PreferredAudioQuality,
                _settings.Current.PreferredVideoResolution).ConfigureAwait(false);

            foreach (var entry in entries) _history.Add(entry);

            // TaskFinished's subscribers (MainViewModel) touch UI-bound
            // state -- including ObservableCollections on the History and
            // Stats tabs, which WPF does not allow updating off the UI
            // thread -- so this has to be marshaled just like the engine's
            // own progress updates rather than invoked from whatever
            // background thread the download just finished on.
            if (uiContext is null) TaskFinished?.Invoke(task, entries);
            else uiContext.Post(_ => TaskFinished?.Invoke(task, entries), null);
        }
        finally
        {
            gate.Release();
        }
    }

    private static void SetStatus(SynchronizationContext? uiContext, DownloadTask task, DownloadStatus status)
    {
        if (uiContext is null) task.Status = status;
        else uiContext.Post(_ => task.Status = status, null);
    }
}
