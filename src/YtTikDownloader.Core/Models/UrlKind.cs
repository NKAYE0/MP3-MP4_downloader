namespace YtTikDownloader.Core.Models;

/// <summary>
/// The kind of resource a pasted URL points to.
/// </summary>
public enum UrlKind
{
    Unknown,
    SingleVideo,
    Playlist,
    Album
}
