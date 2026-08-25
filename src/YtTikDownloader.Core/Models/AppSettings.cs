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

    public bool WriteThumbnailByDefault { get; set; } = true;
    public bool EmbedThumbnailByDefault { get; set; } = true;
    public bool EmbedMetadataByDefault { get; set; } = true;

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
