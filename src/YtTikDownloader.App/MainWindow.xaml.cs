using System.Windows;
using System.Windows.Controls;
using YtTikDownloader.App.Services;
using YtTikDownloader.App.ViewModels;
using YtTikDownloader.App.Views;

namespace YtTikDownloader.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private ClipboardMonitorService? _clipboardMonitor;
    private PlayerView? _playerView;
    private bool _playerViewFailed;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.PlayRequested += OnPlayRequested;
        _viewModel.HistoryVm.PlayRequested += OnPlayRequested;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // The clipboard monitor needs a live window handle, which only
        // exists once the window has been shown, so it's started here
        // rather than in the constructor.
        _clipboardMonitor = new ClipboardMonitorService(this);
        _clipboardMonitor.TextCopied += text =>
        {
            if (_viewModel.Settings.Current.ClipboardDetectionEnabled)
                _viewModel.HandleClipboardTextDetected(text);
        };
    }

    /// <summary>
    /// Constructs the Player tab's content the first time it's needed
    /// (rather than eagerly at startup in XAML) and caches it. If
    /// building it throws -- most likely MediaElement failing because
    /// Windows Media Player components aren't present on this system --
    /// that failure is contained to the Player tab, which shows an
    /// explanatory message instead, rather than taking down the app.
    /// </summary>
    private void EnsurePlayerViewReady()
    {
        if (_playerView is not null || _playerViewFailed) return;

        try
        {
            _playerView = new PlayerView();
            PlayerTabHost.Content = _playerView;
        }
        catch (Exception ex)
        {
            _playerViewFailed = true;
            PlayerTabHost.Content = new TextBlock
            {
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap,
                Text = "The built-in player couldn't start on this system (likely a missing " +
                       "Windows Media Player component). Downloading still works fine -- you can " +
                       "just open finished files with \"Open folder\" instead.\n\nDetails: " + ex.Message
            };
        }
    }

    private void OnPlayRequested(string filePath)
    {
        EnsurePlayerViewReady();
        _playerView?.LoadAndPlay(filePath);
        MainTabControl.SelectedItem = PlayerTabItem;
    }

    private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl.SelectedItem == PlayerTabItem) EnsurePlayerViewReady();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _clipboardMonitor?.Dispose();
    }
}
