using YtTikDownloader.Core.Models;

namespace YtTikDownloader.App.ViewModels;

/// <summary>One checkbox row in a tab's SponsorBlock category list.</summary>
public sealed class SponsorBlockOption : ViewModelBase
{
    public SponsorBlockCategory Category { get; }
    public string DisplayName { get; }

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => SetField(ref _isChecked, value);
    }

    public SponsorBlockOption(SponsorBlockCategory category, string displayName, bool isChecked)
    {
        Category = category;
        DisplayName = displayName;
        _isChecked = isChecked;
    }

    public static List<SponsorBlockOption> CreateDefaultSet(IReadOnlyCollection<SponsorBlockCategory> preChecked) => new()
    {
        new(SponsorBlockCategory.Sponsor, "Sponsor", preChecked.Contains(SponsorBlockCategory.Sponsor)),
        new(SponsorBlockCategory.SelfPromo, "Unpaid/Self Promotion", preChecked.Contains(SponsorBlockCategory.SelfPromo)),
        new(SponsorBlockCategory.Interaction, "Interaction Reminder", preChecked.Contains(SponsorBlockCategory.Interaction)),
        new(SponsorBlockCategory.Intro, "Intro/Intermission", preChecked.Contains(SponsorBlockCategory.Intro)),
        new(SponsorBlockCategory.Outro, "Endcards/Credits", preChecked.Contains(SponsorBlockCategory.Outro)),
        new(SponsorBlockCategory.Preview, "Preview/Recap", preChecked.Contains(SponsorBlockCategory.Preview)),
        new(SponsorBlockCategory.Filler, "Filler Tangent", preChecked.Contains(SponsorBlockCategory.Filler)),
        new(SponsorBlockCategory.MusicOfftopic, "Non-Music Section", preChecked.Contains(SponsorBlockCategory.MusicOfftopic)),
        new(SponsorBlockCategory.PoiHighlight, "Highlight", preChecked.Contains(SponsorBlockCategory.PoiHighlight)),
    };
}
