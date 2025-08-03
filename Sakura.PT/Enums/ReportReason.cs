namespace Sakura.PT.Enums;

/// <summary>
/// Reasons for reporting a torrent.
/// </summary>
public enum ReportReason
{
    /// <summary>
    /// Content is illegal or prohibited.
    /// </summary>
    IllegalContent = 0,

    /// <summary>
    /// Content is miscategorized.
    /// </summary>
    MisleadingCategory,

    /// <summary>
    /// Content is of low quality.
    /// </summary>
    LowQuality,

    /// <summary>
    /// Duplicate torrent.
    /// </summary>
    Duplicate,

    /// <summary>
    /// Dead torrent (no seeders).
    /// </summary>
    DeadTorrent,

    /// <summary>
    /// Other reason, specified in comments.
    /// </summary>
    Other
}
