using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Persists saved download-option presets to presets.json. Same
/// load-everything-keep-in-memory-resave-on-change approach as
/// HistoryRepository; the total count is always tiny (a handful of named
/// presets per category), so there's no need for anything fancier.
/// </summary>
public sealed class DownloadOptionsPresetRepository
{
    private readonly string _path;
    private readonly List<DownloadOptionsPreset> _presets;
    private readonly object _lock = new();

    public DownloadOptionsPresetRepository(string? path = null)
    {
        _path = path ?? AppPaths.PresetsFile;
        _presets = JsonFileStore.Load(_path, () => new List<DownloadOptionsPreset>());
    }

    public IReadOnlyList<DownloadOptionsPreset> GetFor(MediaCategory category)
    {
        lock (_lock)
        {
            return _presets
                .Where(p => p.Category == category)
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// Adds a new preset, or replaces an existing one with the same
    /// category + name (case-insensitive) so re-saving under an existing
    /// name overwrites it instead of creating a duplicate.
    /// </summary>
    public void Save(DownloadOptionsPreset preset)
    {
        lock (_lock)
        {
            _presets.RemoveAll(p =>
                p.Category == preset.Category &&
                string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));

            _presets.Add(preset);
            JsonFileStore.Save(_path, _presets);
        }
    }

    public void Delete(string presetId)
    {
        lock (_lock)
        {
            _presets.RemoveAll(p => p.Id == presetId);
            JsonFileStore.Save(_path, _presets);
        }
    }
}
