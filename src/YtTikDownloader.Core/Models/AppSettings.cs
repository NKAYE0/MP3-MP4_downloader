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
}
