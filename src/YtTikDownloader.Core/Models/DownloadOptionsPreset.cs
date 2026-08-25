namespace YtTikDownloader.Core.Models;

/// <summary>
/// A saved snapshot of one category tab's download options (format,
/// thumbnail/metadata choices, playlist behavior, SponsorBlock
/// selections), so the user doesn't have to re-tick the same boxes every
/// time. Presets are scoped to a single MediaCategory: YouTube, TikTok,
/// and YouTube Music each keep their own separate list, since the
/// combination someone wants for one (e.g. "grab the whole album, embed
/// art") has nothing to do with what they'd want for another (e.g. "just
/// the clip, no metadata").
/// </summary>
public sealed class DownloadOptionsPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public MediaCategory Category { get; set; }

    public DownloadFormat Format { get; set; } = DownloadFormat.Mp4Video;
    public bool WriteThumbnail { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool EmbedMetadata { get; set; }
    public bool DownloadEntirePlaylist { get; set; } = true;
    public string PlaylistItemsText { get; set; } = string.Empty;
    public bool SponsorBlockEnabled { get; set; }
    public List<SponsorBlockCategory> SponsorBlockCategories { get; set; } = new();
}
