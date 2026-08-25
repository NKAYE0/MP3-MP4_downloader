using YtTikDownloader.Core.Models;

namespace YtTikDownloader.Core.Services;

/// <summary>
/// Pure function that turns a DownloadRequest into the exact yt-dlp
/// command-line arguments. Kept separate from the process-running code so
/// the argument logic is easy to read (and test) on its own.
///
/// Markers like "YTDLP_DONE|" and "YTDLP_META|" are our own prefixes
/// baked into --print templates so the engine can pick our lines out of
/// yt-dlp's other console output unambiguously.
/// </summary>
public static class YtDlpArgumentBuilder
{
    public const string DoneMarker = "YTDLP_DONE|";
    public const string MetaMarker = "YTDLP_META|";

    public static List<string> Build(DownloadRequest request, string? ffmpegDirectory, string preferredAudioQuality, string preferredVideoResolution)
    {
        var args = new List<string>
        {
            request.Url,
            "--newline",
            "--no-color",
            "--ignore-errors",
            "--no-warnings",
        };

        if (!string.IsNullOrEmpty(ffmpegDirectory))
        {
            args.Add("--ffmpeg-location");
            args.Add(ffmpegDirectory);
        }

        // Playlist / single-item behavior.
        var isPlaylistLike = request.Kind is UrlKind.Playlist or UrlKind.Album;
        if (isPlaylistLike)
        {
            args.Add(request.DownloadEntirePlaylist ? "--yes-playlist" : "--no-playlist");
            if (request.DownloadEntirePlaylist && !string.IsNullOrWhiteSpace(request.PlaylistItems))
            {
                args.Add("--playlist-items");
                args.Add(request.PlaylistItems);
            }
        }

        // Format selection.
        if (request.Format == DownloadFormat.Mp3Audio)
        {
            args.Add("-x");
            args.Add("--audio-format");
            args.Add("mp3");
            args.Add("--audio-quality");
            args.Add(string.IsNullOrWhiteSpace(preferredAudioQuality) ? "0" : preferredAudioQuality);
        }
        else
        {
            // Prefer merging with an m4a (AAC) audio track over the plain
            // "bv*+ba" fallback: YouTube's overall-best audio is very often
            // an Opus/webm stream, and when that gets muxed straight into
            // an .mp4 container (no re-encode, just remuxed) the file is
            // technically valid but a lot of players -- including Windows'
            // built-in ones and WPF's own MediaElement -- can't decode
            // Opus, so the video plays back with no sound even though the
            // download itself succeeded. AAC/m4a audio is universally
            // compatible with mp4, and YouTube publishes an m4a audio
            // track for virtually every video, so this costs nothing in
            // practice while fixing silent playback. The plain "bv*+ba"
            // and single-file "b" selectors remain as fallbacks for
            // sources (e.g. some TikTok links) that don't offer m4a audio.
            args.Add("-f");
            args.Add("bv*+ba[ext=m4a]/bv*+ba/b");
            args.Add("--merge-output-format");
            args.Add("mp4");
            if (!string.IsNullOrWhiteSpace(preferredVideoResolution))
            {
                args.Add("-S");
                args.Add($"res:{preferredVideoResolution}");
            }
        }

        if (request.WriteThumbnail) args.Add("--write-thumbnail");
        if (request.EmbedThumbnail) args.Add("--embed-thumbnail");
        if (request.EmbedMetadata) args.Add("--embed-metadata");

        if (request.SponsorBlockRemoveCategories.Count > 0)
        {
            args.Add("--sponsorblock-remove");
            args.Add(string.Join(",", request.SponsorBlockRemoveCategories.Select(c => c.ToYtDlpToken())));
        }

        // Output template: group playlist/album downloads into a subfolder
        // named after the playlist so tracks don't scatter loose into the
        // category folder. Playlist/album files also get their position
        // number prefixed onto the filename, zero-padded to 3 digits
        // (e.g. "001 - Song Title [id].mp3") -- Explorer's default view
        // sorts by filename, and %(title)s alone sorts alphabetically,
        // which has nothing to do with track order, so an album would
        // otherwise land on disk in the wrong listening order. A lone,
        // non-playlist download keeps the plain title-only name: there's
        // no order to preserve for a single file.
        var singleFileName = "%(title)s [%(id)s].%(ext)s";
        var playlistFileName = "%(playlist_index)03d - %(title)s [%(id)s].%(ext)s";

        var outputTemplate = isPlaylistLike && request.DownloadEntirePlaylist
            ? Path.Combine(request.OutputFolder, "%(playlist_title,title)s", playlistFileName)
            : Path.Combine(request.OutputFolder, singleFileName);

        args.Add("-o");
        args.Add(outputTemplate);

        // Live metadata for the queue UI (title, thumbnail URL, playlist position).
        args.Add("--print");
        args.Add($"before_dl:{MetaMarker}%(title)s|%(thumbnail)s|%(playlist_index|)s|%(n_entries|)s");

        // Final on-disk path of each file once yt-dlp is done moving/naming it.
        args.Add("--print");
        args.Add($"after_move:{DoneMarker}%(filepath)s");

        return args;
    }
}
