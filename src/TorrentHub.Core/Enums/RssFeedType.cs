namespace TorrentHub.Core.Enums;

/// <summary>
/// Defines the types of RSS feeds available.
/// </summary>
public enum RssFeedType
{
    /// <summary>
    /// The latest torrents.
    /// </summary>
    Latest = 0,

    /// <summary>
    /// Torrents filtered by category.
    /// </summary>
    Category = 1,

    /// <summary>
    /// The user's bookmarked torrents.
    /// </summary>
    Bookmarks = 2,

    /// <summary>
    /// A custom feed based on user-defined criteria.
    /// </summary>
    Custom = 3
}