using System.Windows;
using System.Windows.Controls;
using YtTikDownloader.App.ViewModels;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.Views;

public partial class CategoryTabView : UserControl
{
    public CategoryTabView()
    {
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not CategoryTabViewModel vm) return;

        // Browsers typically offer dragged links as plain text; some also
        // offer them as a "file drop" list if the link was dragged as a
        // shortcut. We handle both.
        string? text = null;
        if (e.Data.GetDataPresent(DataFormats.Text))
            text = e.Data.GetData(DataFormats.Text) as string;

        var urls = new List<string>();
        if (!string.IsNullOrWhiteSpace(text))
            urls.AddRange(UrlClassifier.ExtractUrls(text));

        if (urls.Count == 0 && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                urls.AddRange(files);
        }

        if (urls.Count > 0) vm.EnqueueUrls(urls);
        e.Handled = true;
    }
}
