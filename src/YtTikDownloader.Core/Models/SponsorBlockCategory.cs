namespace YtTikDownloader.Core.Models;

/// <summary>
/// SponsorBlock segment categories that yt-dlp knows how to cut out of a
/// downloaded video. Names match yt-dlp's own --sponsorblock-remove values
/// exactly, since they're passed straight through on the command line.
/// </summary>
public enum SponsorBlockCategory
{
    Sponsor,
    Intro,
    Outro,
    SelfPromo,
    Preview,
    Filler,
    Interaction,
    MusicOfftopic,
    Hook,
    PoiHighlight,
    Chapter
}

public static class SponsorBlockCategoryExtensions
{
    /// <summary>
    /// The exact lowercase token yt-dlp expects for this category on the
    /// --sponsorblock-remove / --sponsorblock-mark command line.
    /// </summary>
    public static string ToYtDlpToken(this SponsorBlockCategory category) => category switch
    {
        SponsorBlockCategory.Sponsor => "sponsor",
        SponsorBlockCategory.Intro => "intro",
        SponsorBlockCategory.Outro => "outro",
        SponsorBlockCategory.SelfPromo => "selfpromo",
        SponsorBlockCategory.Preview => "preview",
        SponsorBlockCategory.Filler => "filler",
        SponsorBlockCategory.Interaction => "interaction",
        SponsorBlockCategory.MusicOfftopic => "music_offtopic",
        SponsorBlockCategory.Hook => "hook",
        SponsorBlockCategory.PoiHighlight => "poi_highlight",
        SponsorBlockCategory.Chapter => "chapter",
        _ => throw new System.ArgumentOutOfRangeException(nameof(category))
    };
}
