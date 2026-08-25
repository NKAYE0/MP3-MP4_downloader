# Builds the release artifact: a single, self-contained YtTikDownloader.exe
# that bundles the .NET runtime, so it runs on any Windows machine with no
# separate install step first. Larger download (roughly 150 MB) in exchange
# for "just download and run" -- the right trade-off for a tool aimed at
# people leaving sketchy converter websites, most of whom won't have (or
# want to install) the .NET Desktop Runtime themselves.
#
# Run from the repo root:
#   .\scripts\publish-release.ps1
#
# Output lands in .\publish\YtTikDownloader.exe (that folder is gitignored --
# it's a build output.)

$ErrorActionPreference = "Stop"

dotnet publish src\YtTikDownloader.App\YtTikDownloader.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish

Write-Host ""
Write-Host "Done -- release exe is at publish\YtTikDownloader.exe" -ForegroundColor Green
