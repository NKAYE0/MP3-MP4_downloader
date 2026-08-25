namespace YtTikDownloader.Core.Models;

/// <summary>
/// Persisted user preferences, saved to settings.json under %AppData%.
/// </summary>
public sealed class AppSettings
{
    public string YouTubeOutputFolder { get; set; } = string.Empty;
    public string TikTokOutputFolder { get; set; } = string.Empty;
    public string YouTubeMusicOutputFolder { get; set; } = string.Empty;

    public DownloadFormat YouTubeDefaultFormat { get; set; } = DownloadFormat.Mp4Video;
    public DownloadFormat TikTokDefaultFormat { get; set; } = DownloadFormat.Mp4Video;
    public DownloadFormat YouTubeMusicDefaultFormat { get; set; } = DownloadFormat.Mp3Audio;

    // Per-category "out of the box" option defaults (before the user has
    // saved any presets). These are separate per category rather than one
    // shared set of fields because the sensible defaults genuinely differ:
    // e.g. TikTok and YouTube Music clips benefit from an embedded
    // thumbnail/cover art by default, whereas YouTube videos already carry
    // their own thumbnail in most players so embedding one is just wasted
    // file size.
    public bool YouTubeWriteThumbnailByDefault { get; set; } = false;
    public bool YouTubeEmbedThumbnailByDefault { get; set; } = false;
    public bool YouTubeEmbedMetadataByDefault { get; set; } = true;

    public bool TikTokWriteThumbnailByDefault { get; set; } = false;
    public bool TikTokEmbedThumbnailByDefault { get; set; } = true;
    public bool TikTokEmbedMetadataByDefault { get; set; } = true;

    public bool YouTubeMusicWriteThumbnailByDefault { get; set; } = false;
    public bool YouTubeMusicEmbedThumbnailByDefault { get; set; } = true;
    public bool YouTubeMusicEmbedMetadataByDefault { get; set; } = true;

    public bool SponsorBlockEnabledByDefault { get; set; } = false;
    public List<SponsorBlockCategory> DefaultSponsorBlockCategories { get; set; } = new()
    {
        SponsorBlockCategory.Sponsor,
        SponsorBlockCategory.SelfPromo,
        SponsorBlockCategory.Interaction
    };

    public int MaxConcurrentDownloads { get; set; } = 2;

    public bool ClipboardDetectionEnabled { get; set; } = true;
    public bool AutoQueueClipboardUrls { get; set; } = false;

    /// <summary>Explicit path override; empty means "auto-detect / use bundled tools folder".</summary>
    public string YtDlpPathOverride { get; set; } = string.Empty;
    public string FfmpegPathOverride { get; set; } = string.Empty;

    public string PreferredAudioQuality { get; set; } = "0"; // yt-dlp --audio-quality, 0 = best
    public string PreferredVideoResolution { get; set; } = "1080";

    /// <summary>
    /// The app's accent color (used for the download progress bar and other
    /// Claude-drawn accents), as an "#AARRGGBB" hex string. This is
    /// separate from Windows' own accent color: WPF's built-in Fluent theme
    /// ties its native controls to the OS accent color and doesn't
    /// currently support overriding that per-app, so this only recolors
    /// the parts of the UI the app draws itself.
    /// </summary>
    public string AccentColorHex { get; set; } = "#FF4FC3F7";
}
