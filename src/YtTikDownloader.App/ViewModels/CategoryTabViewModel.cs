using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using YtTikDownloader.App.Views;
using YtTikDownloader.Core.Models;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.ViewModels;

/// <summary>
/// Drives one of the three category tabs (YouTube / TikTok / YouTube
/// Music): holds that tab's current option selections, a live filtered
/// view of the shared download queue, and the commands the view binds to.
/// </summary>
public sealed class CategoryTabViewModel : ViewModelBase
{
    public MediaCategory Category { get; }
    public string TabTitle { get; }
    public bool ShowSponsorBlockUi => Category != MediaCategory.TikTok;

    private readonly MainViewModel _owner;

    public ObservableCollection<DownloadTask> FilteredQueue { get; } = new();
    public ObservableCollection<SponsorBlockOption> SponsorBlockOptions { get; }

    private string _urlInputText = string.Empty;
    public string UrlInputText
    {
        get => _urlInputText;
        set => SetField(ref _urlInputText, value);
    }

    private bool _isMp4Selected;
    public bool IsMp4Selected
    {
        get => _isMp4Selected;
        set
        {
            if (!SetField(ref _isMp4Selected, value)) return;
            if (value) IsMp3Selected = false;
        }
    }

    private bool _isMp3Selected;
    public bool IsMp3Selected
    {
        get => _isMp3Selected;
        set
        {
            if (!SetField(ref _isMp3Selected, value)) return;
            if (value) IsMp4Selected = false;
        }
    }

    private bool _writeThumbnail;
    public bool WriteThumbnail { get => _writeThumbnail; set => SetField(ref _writeThumbnail, value); }

    private bool _embedThumbnail;
    public bool EmbedThumbnail { get => _embedThumbnail; set => SetField(ref _embedThumbnail, value); }

    private bool _embedMetadata;
    public bool EmbedMetadata { get => _embedMetadata; set => SetField(ref _embedMetadata, value); }

    private bool _downloadEntirePlaylist = true;
    public bool DownloadEntirePlaylist { get => _downloadEntirePlaylist; set => SetField(ref _downloadEntirePlaylist, value); }

    private bool _sponsorBlockEnabled;
    public bool SponsorBlockEnabled { get => _sponsorBlockEnabled; set => SetField(ref _sponsorBlockEnabled, value); }

    private string _playlistItemsText = string.Empty;
    /// <summary>e.g. "1-10" to only grab the first 10 items of a playlist/album. Blank = all.</summary>
    public string PlaylistItemsText { get => _playlistItemsText; set => SetField(ref _playlistItemsText, value); }

    /// <summary>This tab's saved presets only -- YouTube/TikTok/YouTube Music each keep a separate list.</summary>
    public ObservableCollection<DownloadOptionsPreset> Presets { get; } = new();

    private DownloadOptionsPreset? _selectedPreset;
    /// <summary>Picking a preset from the dropdown applies its options immediately.</summary>
    public DownloadOptionsPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetField(ref _selectedPreset, value)) return;
            if (value is not null) ApplyPreset(value);
        }
    }

    public ICommand AddUrlCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand RemoveDownloadCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand SaveAsPresetCommand { get; }
    public ICommand DeleteSelectedPresetCommand { get; }

    public CategoryTabViewModel(MediaCategory category, string tabTitle, MainViewModel owner)
    {
        Category = category;
        TabTitle = tabTitle;
        _owner = owner;

        _isMp4Selected = owner.Settings.DefaultFormatFor(category) == DownloadFormat.Mp4Video;
        _isMp3Selected = !_isMp4Selected;

        _writeThumbnail = owner.Settings.Current.WriteThumbnailByDefault;
        _embedThumbnail = owner.Settings.Current.EmbedThumbnailByDefault;
        _embedMetadata = owner.Settings.Current.EmbedMetadataByDefault;
        _sponsorBlockEnabled = owner.Settings.Current.SponsorBlockEnabledByDefault;

        SponsorBlockOptions = new ObservableCollection<SponsorBlockOption>(
            SponsorBlockOption.CreateDefaultSet(owner.Settings.Current.DefaultSponsorBlockCategories));

        foreach (var task in owner.QueueManager.Queue)
            if (task.Request.Category == Category) FilteredQueue.Add(task);

        owner.QueueManager.Queue.CollectionChanged += OnMasterQueueChanged;

        AddUrlCommand = new RelayCommand(_ => AddFromTextBox());
        CancelDownloadCommand = new RelayCommand(param => { if (param is DownloadTask t) _owner.QueueManager.Cancel(t); });
        RemoveDownloadCommand = new RelayCommand(param => { if (param is DownloadTask t) _owner.QueueManager.RemoveFromQueue(t); });
        PlayCommand = new RelayCommand(param =>
        {
            if (param is DownloadTask { ResultFilePaths.Count: > 0 } t)
                _owner.RequestPlay(t.ResultFilePaths[0]);
        });
        OpenFolderCommand = new RelayCommand(param =>
        {
            if (param is DownloadTask { ResultFilePaths.Count: > 0 } t)
                OpenContainingFolder(t.ResultFilePaths[0]);
        });
        SaveAsPresetCommand = new RelayCommand(_ => SaveAsPreset());
        DeleteSelectedPresetCommand = new RelayCommand(_ => DeleteSelectedPreset(), _ => SelectedPreset is not null);

        RefreshPresets();
    }

    private void OnMasterQueueChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (DownloadTask task in e.NewItems)
                if (task.Request.Category == Category) FilteredQueue.Insert(0, task);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (DownloadTask task in e.OldItems)
                FilteredQueue.Remove(task);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            FilteredQueue.Clear();
        }
    }

    private void AddFromTextBox()
    {
        if (string.IsNullOrWhiteSpace(UrlInputText)) return;

        var urls = UrlClassifier.ExtractUrls(UrlInputText).ToList();
        if (urls.Count == 0) urls.Add(UrlInputText.Trim());

        EnqueueUrls(urls);
        UrlInputText = string.Empty;
    }

    /// <summary>Called from drag-and-drop handling in the view's code-behind.</summary>
    public void EnqueueUrls(IEnumerable<string> rawUrls) => _owner.RouteUrls(rawUrls);

    /// <summary>Builds a DownloadRequest from this tab's current option selections and queues it.</summary>
    public void EnqueueClassified(ClassifiedUrl classified)
    {
        var request = new DownloadRequest
        {
            Url = classified.OriginalUrl,
            Category = Category,
            Kind = classified.Kind,
            Format = IsMp3Selected ? DownloadFormat.Mp3Audio : DownloadFormat.Mp4Video,
            OutputFolder = _owner.Settings.OutputFolderFor(Category),
            WriteThumbnail = WriteThumbnail,
            EmbedThumbnail = EmbedThumbnail,
            EmbedMetadata = EmbedMetadata,
            DownloadEntirePlaylist = DownloadEntirePlaylist,
            PlaylistItems = string.IsNullOrWhiteSpace(PlaylistItemsText) ? null : PlaylistItemsText.Trim(),
            SponsorBlockRemoveCategories = SponsorBlockEnabled
                ? SponsorBlockOptions.Where(o => o.IsChecked).Select(o => o.Category).ToList()
                : Array.Empty<SponsorBlockCategory>()
        };

        _owner.QueueManager.Enqueue(request);
    }

    private void ApplyPreset(DownloadOptionsPreset preset)
    {
        IsMp4Selected = preset.Format == DownloadFormat.Mp4Video;
        IsMp3Selected = !IsMp4Selected;
        WriteThumbnail = preset.WriteThumbnail;
        EmbedThumbnail = preset.EmbedThumbnail;
        EmbedMetadata = preset.EmbedMetadata;
        DownloadEntirePlaylist = preset.DownloadEntirePlaylist;
        PlaylistItemsText = preset.PlaylistItemsText;
        SponsorBlockEnabled = preset.SponsorBlockEnabled;

        foreach (var option in SponsorBlockOptions)
            option.IsChecked = preset.SponsorBlockCategories.Contains(option.Category);
    }

    private void SaveAsPreset()
    {
        // Non-null in practice: this only ever runs from a click inside an
        // already-open category tab, by which point the main window
        // obviously exists.
        var owner = Application.Current.MainWindow!;
        var dialog = new InputDialog(
            "Save preset",
            $"Save the current {TabTitle} download options as a preset:",
            SelectedPreset?.Name ?? string.Empty)
        {
            Owner = owner
        };

        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText;
        var overwriting = Presets.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (overwriting)
        {
            var confirm = MessageBox.Show(owner,
                $"A preset named \"{name}\" already exists for {TabTitle}. Overwrite it?",
                "Overwrite preset?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var preset = new DownloadOptionsPreset
        {
            Id = overwriting
                ? Presets.First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Id
                : Guid.NewGuid().ToString("N"),
            Name = name,
            Category = Category,
            Format = IsMp3Selected ? DownloadFormat.Mp3Audio : DownloadFormat.Mp4Video,
            WriteThumbnail = WriteThumbnail,
            EmbedThumbnail = EmbedThumbnail,
            EmbedMetadata = EmbedMetadata,
            DownloadEntirePlaylist = DownloadEntirePlaylist,
            PlaylistItemsText = PlaylistItemsText,
            SponsorBlockEnabled = SponsorBlockEnabled,
            SponsorBlockCategories = SponsorBlockOptions.Where(o => o.IsChecked).Select(o => o.Category).ToList()
        };

        _owner.PresetRepository.Save(preset);
        RefreshPresets();
        SelectedPreset = Presets.FirstOrDefault(p => p.Id == preset.Id);
    }

    private void DeleteSelectedPreset()
    {
        if (SelectedPreset is null) return;

        var confirm = MessageBox.Show(Application.Current.MainWindow!,
            $"Delete the preset \"{SelectedPreset.Name}\"?", "Delete preset?",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        _owner.PresetRepository.Delete(SelectedPreset.Id);
        SelectedPreset = null;
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        Presets.Clear();
        foreach (var preset in _owner.PresetRepository.GetFor(Category)) Presets.Add(preset);
    }

    private static void OpenContainingFolder(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            else if (Directory.Exists(Path.GetDirectoryName(filePath)))
                Process.Start("explorer.exe", Path.GetDirectoryName(filePath)!);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            // Best-effort only; nothing useful to do if Explorer can't be launched.
        }
    }
}
