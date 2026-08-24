using System.Windows;
using YtTikDownloader.App.Services;
using YtTikDownloader.App.ViewModels;

namespace YtTikDownloader.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private ClipboardMonitorService? _clipboardMonitor;

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

    private void OnPlayRequested(string filePath)
    {
        PlayerViewControl.LoadAndPlay(filePath);
        MainTabControl.SelectedItem = PlayerTabItem;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _clipboardMonitor?.Dispose();
    }
}
