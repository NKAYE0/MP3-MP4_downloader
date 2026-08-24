using System.Diagnostics;
using System.Text.RegularExpressions;
using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Runs one yt-dlp process for a DownloadTask, streaming progress back
/// onto the task's own bindable properties as it goes, and returns a
/// HistoryEntry describing the outcome once the process exits.
/// </summary>
public sealed class YtDlpDownloadEngine
{
    private static readonly Regex PercentRegex = new(@"\[download\]\s+([\d.]+)%", RegexOptions.Compiled);
    private static readonly Regex SpeedRegex = new(@"at\s+([\d.]+\S*/s)", RegexOptions.Compiled);
    private static readonly Regex EtaRegex = new(@"ETA\s+(\S+)", RegexOptions.Compiled);

    private readonly YtDlpBinaryManager _binaryManager;

    public YtDlpDownloadEngine(YtDlpBinaryManager binaryManager)
    {
        _binaryManager = binaryManager;
    }

    public async Task<HistoryEntry> RunAsync(DownloadTask task, string preferredAudioQuality, string preferredVideoResolution)
    {
        var ytDlpPath = _binaryManager.ResolveYtDlpPath();
        if (ytDlpPath is null)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = "yt-dlp.exe was not found. Go to Settings and click \"Download/Update tools\" first.";
            return FailedEntry(task);
        }

        var ffmpegPath = _binaryManager.ResolveFfmpegPath();
        var ffmpegDirectory = ffmpegPath is not null ? Path.GetDirectoryName(ffmpegPath) : null;
        if (ffmpegPath is null)
        {
            // Not immediately fatal for a plain video download, but mp3
            // extraction / thumbnail embedding / SponsorBlock cutting all
            // need it, so warn loudly via the task's error slot without
            // failing yet -- yt-dlp itself will error out if it truly can't
            // proceed, and that message will surface via ErrorMessage below.
            task.ErrorMessage = "Warning: ffmpeg.exe not found. Some options (mp3 conversion, thumbnail embedding, SponsorBlock) may fail.";
        }

        Directory.CreateDirectory(task.Request.OutputFolder);

        var args = YtDlpArgumentBuilder.Build(task.Request, ffmpegDirectory, preferredAudioQuality, preferredVideoResolution);

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var resultPaths = new List<string>();
        var stderrLines = new List<string>();
        var ct = task.CancellationSource.Token;
        var itemsSeen = 0;

        task.Status = DownloadStatus.FetchingInfo;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            HandleStdOutLine(e.Data, task, resultPaths, ref itemsSeen);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stderrLines.Add(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await using var registration = ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { /* already exited */ }
            });

            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = $"Failed to run yt-dlp: {ex.Message}";
            return FailedEntry(task);
        }

        if (ct.IsCancellationRequested)
        {
            task.Status = DownloadStatus.Canceled;
            return FailedEntry(task, "Canceled by user.");
        }

        if (process.ExitCode != 0 && resultPaths.Count == 0)
        {
            task.Status = DownloadStatus.Failed;
            var tail = string.Join(Environment.NewLine, stderrLines.TakeLast(5));
            task.ErrorMessage = string.IsNullOrWhiteSpace(tail)
                ? $"yt-dlp exited with code {process.ExitCode}."
                : tail;
            return FailedEntry(task);
        }

        foreach (var p in resultPaths) task.ResultFilePaths.Add(p);
        task.ProgressPercent = 100;
        task.Status = DownloadStatus.Completed; // set last so HasResultFiles (re-announced on Status change) sees the populated list

        long totalBytes = 0;
        foreach (var p in resultPaths)
        {
            try { if (File.Exists(p)) totalBytes += new FileInfo(p).Length; }
            catch (IOException) { }
        }

        return new HistoryEntry
        {
            Title = string.IsNullOrWhiteSpace(task.Title) ? task.Request.Url : task.Title,
            Url = task.Request.Url,
            Category = task.Request.Category,
            Format = task.Request.Format,
            Success = true,
            FilePaths = resultPaths,
            TotalFileSizeBytes = totalBytes,
            SponsorBlockApplied = task.Request.SponsorBlockRemoveCategories.Count > 0
        };
    }

    private static void HandleStdOutLine(string line, DownloadTask task, List<string> resultPaths, ref int itemsSeen)
    {
        if (line.StartsWith(YtDlpArgumentBuilder.MetaMarker, StringComparison.Ordinal))
        {
            var payload = line[YtDlpArgumentBuilder.MetaMarker.Length..];
            var parts = payload.Split('|');
            var title = parts.Length > 0 ? parts[0] : task.Title;
            var index = parts.Length > 2 ? parts[2] : string.Empty;
            var total = parts.Length > 3 ? parts[3] : string.Empty;

            if (!string.IsNullOrWhiteSpace(title)) task.Title = title;
            task.Status = DownloadStatus.Downloading;
            task.CurrentItemLabel = (!string.IsNullOrWhiteSpace(index) && !string.IsNullOrWhiteSpace(total))
                ? $"Item {index} of {total}"
                : null;
            return;
        }

        if (line.StartsWith(YtDlpArgumentBuilder.DoneMarker, StringComparison.Ordinal))
        {
            var path = line[YtDlpArgumentBuilder.DoneMarker.Length..].Trim();
            if (!string.IsNullOrWhiteSpace(path)) resultPaths.Add(path);
            itemsSeen++;
            task.ProgressPercent = 0; // reset for next item in a playlist
            return;
        }

        if (line.Contains("[Merger]") || line.Contains("[ExtractAudio]") || line.Contains("[Metadata]") || line.Contains("[EmbedThumbnail]") || line.Contains("[SponsorBlock]"))
        {
            task.Status = DownloadStatus.PostProcessing;
            return;
        }

        var percentMatch = PercentRegex.Match(line);
        if (percentMatch.Success && double.TryParse(percentMatch.Groups[1].Value, out var pct))
        {
            task.Status = DownloadStatus.Downloading;
            task.ProgressPercent = pct;

            var speedMatch = SpeedRegex.Match(line);
            task.SpeedText = speedMatch.Success ? speedMatch.Groups[1].Value : task.SpeedText;

            var etaMatch = EtaRegex.Match(line);
            task.EtaText = etaMatch.Success ? etaMatch.Groups[1].Value : task.EtaText;
        }
    }

    private static HistoryEntry FailedEntry(DownloadTask task, string? overrideMessage = null) => new()
    {
        Title = string.IsNullOrWhiteSpace(task.Title) ? task.Request.Url : task.Title,
        Url = task.Request.Url,
        Category = task.Request.Category,
        Format = task.Request.Format,
        Success = false,
        ErrorMessage = overrideMessage ?? task.ErrorMessage,
        FilePaths = new List<string>(),
        TotalFileSizeBytes = 0
    };
}
