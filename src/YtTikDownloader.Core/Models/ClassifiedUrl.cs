namespace YtTikDownloader.Core.Models;

/// <summary>
/// Result of inspecting a pasted/dropped/clipboard URL to figure out
/// which category tab it belongs to and what kind of resource it is.
/// </summary>
public sealed class ClassifiedUrl
{
    public required string OriginalUrl { get; init; }
    public bool IsSupported { get; init; }
    public MediaCategory? Category { get; init; }
    public UrlKind Kind { get; init; } = UrlKind.Unknown;

    public static ClassifiedUrl Unsupported(string url) => new()
    {
        OriginalUrl = url,
        IsSupported = false,
        Category = null,
        Kind = UrlKind.Unknown
    };
}
