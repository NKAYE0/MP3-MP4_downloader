using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Loads/saves the single AppSettings object. Keeps an in-memory copy so
/// the UI can read it synchronously after startup and save it back
/// whenever the user changes something.
/// </summary>
public sealed class SettingsService
{
    private readonly string _path;
    public AppSettings Current { get; private set; }

    public SettingsService(string? path = null)
    {
        _path = path ?? AppPaths.SettingsFile;
        Current = JsonFileStore.Load(_path, CreateDefaultSettings);
    }

    public void Save() => JsonFileStore.Save(_path, Current);

    private static AppSettings CreateDefaultSettings()
    {
        var settings = new AppSettings();
        settings.YouTubeOutputFolder = Path.Combine(AppPaths.DefaultDownloadsRoot, "YouTube");
        settings.TikTokOutputFolder = Path.Combine(AppPaths.DefaultDownloadsRoot, "TikTok");
        settings.YouTubeMusicOutputFolder = Path.Combine(AppPaths.DefaultDownloadsRoot, "YouTube Music");
        return settings;
    }

    public string OutputFolderFor(MediaCategory category) => category switch
    {
        MediaCategory.YouTube => Current.YouTubeOutputFolder,
        MediaCategory.TikTok => Current.TikTokOutputFolder,
        MediaCategory.YouTubeMusic => Current.YouTubeMusicOutputFolder,
        _ => AppPaths.DefaultDownloadsRoot
    };

    public DownloadFormat DefaultFormatFor(MediaCategory category) => category switch
    {
        MediaCategory.YouTube => Current.YouTubeDefaultFormat,
        MediaCategory.TikTok => Current.TikTokDefaultFormat,
        MediaCategory.YouTubeMusic => Current.YouTubeMusicDefaultFormat,
        _ => DownloadFormat.Mp4Video
    };

    public bool WriteThumbnailDefaultFor(MediaCategory category) => category switch
    {
        MediaCategory.YouTube => Current.YouTubeWriteThumbnailByDefault,
        MediaCategory.TikTok => Current.TikTokWriteThumbnailByDefault,
        MediaCategory.YouTubeMusic => Current.YouTubeMusicWriteThumbnailByDefault,
        _ => false
    };

    public bool EmbedThumbnailDefaultFor(MediaCategory category) => category switch
    {
        MediaCategory.YouTube => Current.YouTubeEmbedThumbnailByDefault,
        MediaCategory.TikTok => Current.TikTokEmbedThumbnailByDefault,
        MediaCategory.YouTubeMusic => Current.YouTubeMusicEmbedThumbnailByDefault,
        _ => false
    };

    public bool EmbedMetadataDefaultFor(MediaCategory category) => category switch
    {
        MediaCategory.YouTube => Current.YouTubeEmbedMetadataByDefault,
        MediaCategory.TikTok => Current.TikTokEmbedMetadataByDefault,
        MediaCategory.YouTubeMusic => Current.YouTubeMusicEmbedMetadataByDefault,
        _ => true
    };
}
