using System.IO;
using System.Net.Http;
using System.Windows.Input;
using Microsoft.Win32;
using YtTikDownloader.App.Services;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settings;
    private readonly YtDlpBinaryManager _binaryManager;
    private readonly DownloadQueueManager _queueManager;

    public List<int> ConcurrencyOptions { get; } = new() { 1, 2, 3, 4, 5 };

    public List<ComboOption> AudioQualityOptions { get; } = new()
    {
        new ComboOption("0", "Best"),
        new ComboOption("2", "High"),
        new ComboOption("5", "Medium"),
        new ComboOption("9", "Smallest"),
    };

    public List<string> VideoResolutionOptions { get; } = new() { "2160", "1440", "1080", "720", "480" };

    public string YouTubeOutputFolder
    {
        get => _settings.Current.YouTubeOutputFolder;
        set { _settings.Current.YouTubeOutputFolder = value; OnPropertyChanged(); Save(); }
    }

    public string TikTokOutputFolder
    {
        get => _settings.Current.TikTokOutputFolder;
        set { _settings.Current.TikTokOutputFolder = value; OnPropertyChanged(); Save(); }
    }

    public string YouTubeMusicOutputFolder
    {
        get => _settings.Current.YouTubeMusicOutputFolder;
        set { _settings.Current.YouTubeMusicOutputFolder = value; OnPropertyChanged(); Save(); }
    }

    public int MaxConcurrentDownloads
    {
        get => _settings.Current.MaxConcurrentDownloads;
        set
        {
            _settings.Current.MaxConcurrentDownloads = value;
            OnPropertyChanged();
            Save();
            _queueManager.UpdateConcurrency(value);
        }
    }

    public bool ClipboardDetectionEnabled
    {
        get => _settings.Current.ClipboardDetectionEnabled;
        set { _settings.Current.ClipboardDetectionEnabled = value; OnPropertyChanged(); Save(); }
    }

    public bool AutoQueueClipboardUrls
    {
        get => _settings.Current.AutoQueueClipboardUrls;
        set { _settings.Current.AutoQueueClipboardUrls = value; OnPropertyChanged(); Save(); }
    }

    public string PreferredAudioQuality
    {
        get => _settings.Current.PreferredAudioQuality;
        set { _settings.Current.PreferredAudioQuality = value; OnPropertyChanged(); Save(); }
    }

    public string PreferredVideoResolution
    {
        get => _settings.Current.PreferredVideoResolution;
        set { _settings.Current.PreferredVideoResolution = value; OnPropertyChanged(); Save(); }
    }

    /// <summary>
    /// "#AARRGGBB". The swatch preview in Settings binds to the app-level
    /// AppAccentBrush resource (kept in sync by AccentColorService) rather
    /// than to this string directly, so it updates the instant a new color
    /// is picked.
    /// </summary>
    public string AccentColorHex
    {
        get => _settings.Current.AccentColorHex;
        set
        {
            _settings.Current.AccentColorHex = value;
            OnPropertyChanged();
            Save();
            AccentColorService.Apply(value);
        }
    }

    private string _ytDlpStatusText = "Checking...";
    public string YtDlpStatusText { get => _ytDlpStatusText; private set => SetField(ref _ytDlpStatusText, value); }

    private string _ffmpegStatusText = "Checking...";
    public string FfmpegStatusText { get => _ffmpegStatusText; private set => SetField(ref _ffmpegStatusText, value); }

    private bool _isUpdatingTools;
    public bool IsUpdatingTools { get => _isUpdatingTools; private set => SetField(ref _isUpdatingTools, value); }

    private string _toolsUpdateMessage = string.Empty;
    public string ToolsUpdateMessage { get => _toolsUpdateMessage; private set => SetField(ref _toolsUpdateMessage, value); }

    public ICommand BrowseYouTubeFolderCommand { get; }
    public ICommand BrowseTikTokFolderCommand { get; }
    public ICommand BrowseYouTubeMusicFolderCommand { get; }
    public ICommand DownloadOrUpdateToolsCommand { get; }
    public ICommand RefreshToolStatusCommand { get; }
    public ICommand ChooseAccentColorCommand { get; }
    public ICommand ResetAccentColorCommand { get; }

    public SettingsViewModel(SettingsService settings, YtDlpBinaryManager binaryManager, DownloadQueueManager queueManager)
    {
        _settings = settings;
        _binaryManager = binaryManager;
        _queueManager = queueManager;

        BrowseYouTubeFolderCommand = new RelayCommand(_ => BrowseFolder(v => YouTubeOutputFolder = v));
        BrowseTikTokFolderCommand = new RelayCommand(_ => BrowseFolder(v => TikTokOutputFolder = v));
        BrowseYouTubeMusicFolderCommand = new RelayCommand(_ => BrowseFolder(v => YouTubeMusicOutputFolder = v));
        RefreshToolStatusCommand = new RelayCommand(_ => RefreshToolStatus());
        DownloadOrUpdateToolsCommand = new RelayCommand(async _ => await DownloadOrUpdateToolsAsync());
        ChooseAccentColorCommand = new RelayCommand(_ => ChooseAccentColor());
        ResetAccentColorCommand = new RelayCommand(_ => AccentColorHex = AccentColorService.DefaultHex);

        RefreshToolStatus();
    }

    private void RefreshToolStatus()
    {
        var ytDlp = _binaryManager.ResolveYtDlpPath();
        YtDlpStatusText = ytDlp is not null ? $"Found: {ytDlp}" : "Not found — click \"Download/Update tools\" below.";

        var ffmpeg = _binaryManager.ResolveFfmpegPath();
        FfmpegStatusText = ffmpeg is not null ? $"Found: {ffmpeg}" : "Not found — click \"Download/Update tools\" below.";
    }

    private async Task DownloadOrUpdateToolsAsync()
    {
        IsUpdatingTools = true;
        ToolsUpdateMessage = string.Empty;
        var progress = new Progress<string>(msg => ToolsUpdateMessage = msg);

        try
        {
            await _binaryManager.DownloadOrUpdateYtDlpAsync(progress, CancellationToken.None);
            await _binaryManager.DownloadOrUpdateFfmpegAsync(progress, CancellationToken.None);
            ToolsUpdateMessage = "yt-dlp and ffmpeg are up to date.";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            ToolsUpdateMessage = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingTools = false;
            RefreshToolStatus();
        }
    }

    private static void BrowseFolder(Action<string> onSelected)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a download folder" };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            onSelected(dialog.FolderName);
    }

    /// <summary>
    /// WPF has no built-in color-picker dialog, so this uses
    /// System.Windows.Forms.ColorDialog -- part of the .NET Windows Desktop
    /// runtime, not a NuGet package -- opened straight to its full
    /// hue/saturation/luminosity picker so the user can choose literally
    /// any color, not just a preset swatch.
    /// </summary>
    private void ChooseAccentColor()
    {
        var current = AccentColorService.ParseOrDefault(AccentColorHex);

        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B),
            AllowFullOpen = true,
            FullOpen = true,
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dialog.Color;
            AccentColorHex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void Save() => _settings.Save();
}
