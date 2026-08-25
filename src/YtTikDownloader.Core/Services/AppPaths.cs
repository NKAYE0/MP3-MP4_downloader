namespace YtTikDownloader.Core.Services;

/// <summary>
/// Central place for every folder the app writes to under the user's
/// %AppData%, plus the default per-category download folders under
/// "Videos\YtTikDownloader". Keeping this in one file avoids the paths
/// drifting apart between services.
/// </summary>
public static class AppPaths
{
    public static string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YtTikDownloader");

    public static string SettingsFile => Path.Combine(AppDataRoot, "settings.json");
    public static string HistoryFile => Path.Combine(AppDataRoot, "history.json");
    public static string ToolsFolder => Path.Combine(AppDataRoot, "tools");
    public static string YtDlpExePath => Path.Combine(ToolsFolder, "yt-dlp.exe");
    public static string FfmpegExePath => Path.Combine(ToolsFolder, "ffmpeg.exe");
    public static string FfprobeExePath => Path.Combine(ToolsFolder, "ffprobe.exe");

    public static string DefaultDownloadsRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        "YtTikDownloader");

    public static void EnsureCoreFoldersExist()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(ToolsFolder);
    }
}
