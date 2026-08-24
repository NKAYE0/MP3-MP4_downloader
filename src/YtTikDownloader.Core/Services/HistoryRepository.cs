using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Persists download history to history.json. Keeps the full list in
/// memory (fine for a personal app -- even a few thousand entries is a
/// small JSON file) and re-saves the whole file on every change.
/// </summary>
public sealed class HistoryRepository
{
    private readonly string _path;
    private readonly List<HistoryEntry> _entries;
    private readonly object _lock = new();

    public HistoryRepository(string? path = null)
    {
        _path = path ?? AppPaths.HistoryFile;
        _entries = JsonFileStore.Load(_path, () => new List<HistoryEntry>());
    }

    public void Add(HistoryEntry entry)
    {
        lock (_lock)
        {
            _entries.Insert(0, entry); // newest first
            JsonFileStore.Save(_path, _entries);
        }
    }

    public void Remove(string entryId)
    {
        lock (_lock)
        {
            _entries.RemoveAll(e => e.Id == entryId);
            JsonFileStore.Save(_path, _entries);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            JsonFileStore.Save(_path, _entries);
        }
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    /// <summary>
    /// Simple case-insensitive search over title and URL, optionally
    /// filtered by category. Good enough for a personal history of a few
    /// thousand items without needing a real search index.
    /// </summary>
    public IReadOnlyList<HistoryEntry> Search(string? query, MediaCategory? category = null)
    {
        lock (_lock)
        {
            IEnumerable<HistoryEntry> results = _entries;

            if (category.HasValue)
                results = results.Where(e => e.Category == category.Value);

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(e =>
                    e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    e.Url.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return results.ToList();
        }
    }
}
