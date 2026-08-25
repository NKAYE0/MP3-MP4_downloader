using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using YtTikDownloader.Core.Models;
using YtTikDownloader.Core.Services;

namespace YtTikDownloader.App.ViewModels;

public sealed class HistoryViewModel : ViewModelBase
{
    private readonly HistoryRepository _history;

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) Refresh(); }
    }

    public ICommand PlayCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand ClearAllCommand { get; }

    public event Action<string>? PlayRequested;

    public HistoryViewModel(HistoryRepository history)
    {
        _history = history;

        PlayCommand = new RelayCommand(param =>
        {
            if (param is HistoryEntry { FilePaths.Count: > 0 } e) PlayRequested?.Invoke(e.FilePaths[0]);
        });
        OpenFolderCommand = new RelayCommand(param =>
        {
            if (param is HistoryEntry { FilePaths.Count: > 0 } e) OpenContainingFolder(e.FilePaths[0]);
        });
        RemoveCommand = new RelayCommand(param =>
        {
            if (param is HistoryEntry e)
            {
                _history.Remove(e.Id);
                Refresh();
            }
        });
        ClearAllCommand = new RelayCommand(_ =>
        {
            _history.Clear();
            Refresh();
        });

        Refresh();
    }

    public void Refresh()
    {
        var results = _history.Search(SearchText);
        Entries.Clear();
        foreach (var entry in results) Entries.Add(entry);
    }

    private static void OpenContainingFolder(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
        }
    }
}
