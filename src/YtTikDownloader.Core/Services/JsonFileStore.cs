using System.Text.Json;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Minimal generic helper for loading/saving a single object as
/// indented JSON. Used instead of a database engine (SQLite etc.) so the
/// whole app has zero third-party NuGet dependencies -- for a
/// single-user desktop app, a flat JSON file is simpler and just as
/// reliable, and it avoids native-library packaging headaches entirely.
/// </summary>
public static class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static T Load<T>(string path, Func<T> createDefault)
    {
        try
        {
            if (!File.Exists(path)) return createDefault();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return createDefault();
            return JsonSerializer.Deserialize<T>(json, Options) ?? createDefault();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file: fall back to defaults rather than
            // crashing the app on startup.
            return createDefault();
        }
    }

    public static void Save<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, Options);

        // Write to a temp file then move into place, so a crash or power
        // loss mid-write can't corrupt the existing file.
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
