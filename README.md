# MP3/MP4 Downloader

A free, open-source Windows desktop app for downloading YouTube, YouTube Music, and
TikTok content as MP4/MP3 — built as a clean alternative to the ad- and malware-riddled
"yt to mp3" conversion websites. No ads, no bundled junk, nothing hosted that can go
down or get monetized against you. Built in C# / WPF (.NET 8), with
[yt-dlp](https://github.com/yt-dlp/yt-dlp) and [ffmpeg](https://ffmpeg.org/) doing the
actual downloading/converting under the hood.

## Disclaimer

This is a general-purpose client, similar in spirit to yt-dlp itself (which it's built
on): it downloads whatever URL you give it. It's on you, the user, to make sure your use
complies with the terms of service of whatever site you're downloading from and with
copyright law in your jurisdiction — this project doesn't grant you any rights to
content you don't already have the right to download, and isn't intended to facilitate
copyright infringement.

## Features

- Three separate tabs: **YouTube**, **TikTok**, **YouTube Music** — each with its own
  download options and queue. Pasting/dropping a link routes it to the right tab
  automatically even if you're on a different one.
- MP4 (video) or MP3 (audio-only) output, per download.
- Whole-playlist and whole-album downloads, with an optional item range (e.g. `1-10`).
- Thumbnail/album-art download (as a separate file and/or embedded in the media file),
  plus metadata tag embedding (title/artist/etc.).
- SponsorBlock integration — optionally strip sponsor/self-promo/intro/outro/etc.
  segments out of the downloaded video (YouTube only; the SponsorBlock database doesn't
  cover TikTok, so that option is hidden on the TikTok tab).
- Drag & drop URLs onto any tab.
- Clipboard detection — copy a supported link and the app either queues it automatically
  or shows a one-click "Add" banner, depending on your Settings.
- Built-in media player (Player tab) to preview anything you've downloaded.
- Download/search history with a search box, and a Statistics tab with totals,
  breakdowns by category/format, and total disk space used.
- Settings: per-category download folders, concurrent download limit, default audio
  quality/video resolution, clipboard behavior, and a "Download/Update tools" button
  that fetches yt-dlp.exe and ffmpeg.exe for you.

## Why this should keep working over time

Unlike a hosted "yt to mp3" website, there's no server here to go down, get DMCA'd
offline, or get sold out to ad networks — everything runs locally on your own machine.
The one thing that actually breaks over time is yt-dlp's extractors, whenever YouTube or
TikTok change their internals; the in-app **"Download/Update tools"** button fetches the
latest yt-dlp/ffmpeg on demand, so keeping the app working is a one-click action rather
than waiting on a new install.

## Project layout

```
YtTikDownloader.sln
src/
  YtTikDownloader.Core/   Plain C# class library: all the actual logic (URL
                          classification, yt-dlp process handling, settings/history
                          persistence, the download queue). No third-party NuGet
                          packages — everything is built on the .NET base class
                          library on purpose, to keep the dependency footprint small
                          and avoid version-compatibility headaches.
  YtTikDownloader.App/    The WPF UI. Thin hand-rolled MVVM (no MVVM toolkit
                          dependency either) — ViewModels + XAML views that bind
                          to the Core services.
```

Settings, history, and the downloaded yt-dlp.exe/ffmpeg.exe live under
`%AppData%\YtTikDownloader\`. Downloaded media defaults to
`%UserProfile%\Videos\YtTikDownloader\<Category>\`, which you can change any time
in Settings.

## Building & running

You'll need [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows
(WPF only runs on Windows). Then either:

- Open `YtTikDownloader.sln` in Visual Studio 2022 (17.8+) and press F5, or
- From a terminal: `dotnet run --project src/YtTikDownloader.App`

**First run:** go to the **Settings** tab and click **"Download/Update tools"**. This
fetches the current `yt-dlp.exe` and `ffmpeg.exe` into the app's tools folder — nothing
downloads or updates automatically in the background, so this is a one-time (well,
whenever-you-want-to-update) manual step. If you already have yt-dlp/ffmpeg installed
somewhere, you can instead point Settings' path override fields at your existing copies
(that override field isn't in the UI yet as a browsable file picker — the underlying
setting exists in `AppSettings.YtDlpPathOverride` / `FfmpegPathOverride` if you want to
wire up a picker for it, or just place your own `yt-dlp.exe`/`ffmpeg.exe` directly into
`%AppData%\YtTikDownloader\tools\` and the app will find them there).

## Current status / honest caveats

This app was built and reviewed in a cloud Linux sandbox, which let its author
compile-check and fix the `YtTikDownloader.Core` project directly — that's where most of
the actual logic lives, and it builds clean with zero warnings. The WPF UI project
(`YtTikDownloader.App`) could not be compile-checked the same way there (compiling a WPF
project, even without running it, needs Windows Desktop reference assemblies that
sandbox couldn't fetch), so it's had a careful manual review but not an actual compiler
pass yet. It should build normally in Visual Studio on Windows.

A couple of other things worth knowing:

- **ffmpeg auto-download** pulls from the BtbN/FFmpeg-Builds GitHub project's current
  release asset name. If that project ever renames its file, the download will fail
  with a clear error message rather than silently doing the wrong thing.
- Real-world testing (progress tracking, SponsorBlock accuracy, large playlists, etc.)
  is still needed — the core download/queue mechanics are built directly on yt-dlp's
  documented command-line interface, but this hasn't had an end-to-end run yet.
- No installer or CI/CD pipeline yet — currently source-only, build-it-yourself. A
  packaged release (GitHub Actions building an installer on tagged releases) is a
  planned next step, not yet done.
- No LICENSE file yet — until one is added, standard copyright applies and the code
  isn't technically usable/redistributable by others despite being public. MIT is the
  likely pick for a tool like this (permissive, minimal obligations); yt-dlp itself is
  Unlicense (public domain), and the ffmpeg build this app downloads is a GPL build,
  invoked as a separate process rather than linked, which is the standard way to keep an
  app's own license independent of ffmpeg's.

## Where to go next

Reasonable next steps: a LICENSE file, GitHub Actions CI + installer releases, an
actual folder-picker for the yt-dlp/ffmpeg path overrides, a system tray icon so the
app can keep downloading in the background, remembering the last-selected tab, and a
proper app icon.
