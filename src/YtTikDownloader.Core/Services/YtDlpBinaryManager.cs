using System.IO.Compression;
using System.Net.Http;
using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Locates yt-dlp.exe and ffmpeg.exe, and can fetch them fresh from their
/// official release locations when missing or when the user asks to
/// update. Nothing here runs automatically on every startup -- the app
/// only downloads when the user presses "Download/Update tools" in
/// Settings, so a flaky connection never blocks opening the app.
///
/// Sources (verified against the projects' own docs/README at the time
/// this was written -- if either project renames its release assets in
/// the future, only this file needs to change):
///   yt-dlp:  https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe
///   ffmpeg:  https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip
/// </summary>
public sealed class YtDlpBinaryManager
{
    private const string YtDlpDownloadUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    private const string FfmpegZipDownloadUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

    private readonly SettingsService _settings;

    public YtDlpBinaryManager(SettingsService settings)
    {
        _settings = settings;
    }

    public string? ResolveYtDlpPath()
    {
        var overridePath = _settings.Current.YtDlpPathOverride;
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;

        if (File.Exists(AppPaths.YtDlpExePath))
            return AppPaths.YtDlpExePath;

        return FindOnPath("yt-dlp.exe") ?? FindOnPath("yt-dlp");
    }

    public string? ResolveFfmpegPath()
    {
        var overridePath = _settings.Current.FfmpegPathOverride;
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;

        if (File.Exists(AppPaths.FfmpegExePath))
            return AppPaths.FfmpegExePath;

        return FindOnPath("ffmpeg.exe") ?? FindOnPath("ffmpeg");
    }

    public bool AreToolsAvailable() => ResolveYtDlpPath() is not null && ResolveFfmpegPath() is not null;

    public async Task DownloadOrUpdateYtDlpAsync(IProgress<string>? status, CancellationToken ct)
    {
        AppPaths.EnsureCoreFoldersExist();
        status?.Report("Downloading latest yt-dlp.exe...");

        using var client = CreateHttpClient();
        using var response = await client.GetAsync(YtDlpDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempPath = AppPaths.YtDlpExePath + ".download";
        await using (var fileStream = File.Create(tempPath))
        await using (var httpStream = await response.Content.ReadAsStreamAsync(ct))
        {
            await httpStream.CopyToAsync(fileStream, ct);
        }

        File.Move(tempPath, AppPaths.YtDlpExePath, overwrite: true);
        status?.Report("yt-dlp.exe updated.");
    }

    public async Task DownloadOrUpdateFfmpegAsync(IProgress<string>? status, CancellationToken ct)
    {
        AppPaths.EnsureCoreFoldersExist();
        status?.Report("Downloading latest ffmpeg build (this is a larger file, may take a minute)...");

        using var client = CreateHttpClient();
        using var response = await client.GetAsync(FfmpegZipDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempZipPath = Path.Combine(AppPaths.ToolsFolder, "ffmpeg-download.zip");
        await using (var fileStream = File.Create(tempZipPath))
        await using (var httpStream = await response.Content.ReadAsStreamAsync(ct))
        {
            await httpStream.CopyToAsync(fileStream, ct);
        }

        status?.Report("Extracting ffmpeg.exe...");
        var extractDir = Path.Combine(AppPaths.ToolsFolder, "ffmpeg-extract-tmp");
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
        Directory.CreateDirectory(extractDir);
        ZipFile.ExtractToDirectory(tempZipPath, extractDir);

        var ffmpegExe = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (ffmpegExe is null)
        {
            throw new InvalidOperationException(
                "Downloaded ffmpeg archive did not contain ffmpeg.exe in the expected layout. " +
                "The build's folder structure may have changed -- please report this so the download URL can be fixed, " +
                "or download ffmpeg manually and set its path in Settings.");
        }

        File.Copy(ffmpegExe, AppPaths.FfmpegExePath, overwrite: true);

        // Best-effort cleanup; failure to delete temp files isn't fatal.
        try { File.Delete(tempZipPath); } catch (IOException) { }
        try { Directory.Delete(extractDir, recursive: true); } catch (IOException) { }

        status?.Report("ffmpeg.exe updated.");
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("YtTikDownloader/1.0");
        return client;
    }

    private static string? FindOnPath(string exeName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar)) return null;

        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir, exeName);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException)
            {
                // Malformed PATH entry; skip it.
            }
        }
        return null;
    }
}
