using System.Windows.Controls;
using System.Windows.Media;

namespace YtTikDownloader.App.Views;

/// <summary>
/// A small built-in player so users can preview downloads without leaving
/// the app. Deliberately kept as plain code-behind rather than MVVM:
/// MediaElement's playback API (Play/Pause/Stop, Source) isn't friendly
/// to data binding, and this view has no state worth unit testing.
/// </summary>
public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
    }

    public void LoadAndPlay(string filePath)
    {
        if (!File.Exists(filePath))
        {
            NowPlayingText.Text = $"File not found: {filePath}";
            return;
        }

        NowPlayingText.Text = Path.GetFileName(filePath);
        Player.Source = new Uri(filePath, UriKind.Absolute);
        Player.Play();
    }

    private void OnPlayClick(object sender, System.Windows.RoutedEventArgs e) => Player.Play();
    private void OnPauseClick(object sender, System.Windows.RoutedEventArgs e) => Player.Pause();
    private void OnStopClick(object sender, System.Windows.RoutedEventArgs e) => Player.Stop();

    private void OnVolumeChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        Player.Volume = e.NewValue;
    }
}
