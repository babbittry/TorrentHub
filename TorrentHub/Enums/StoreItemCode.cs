namespace TorrentHub.Enums;

/// <summary>
/// Unique identifier for different types of items available in the store.
/// This allows the code to know what action to perform when an item is purchased.
/// </summary>
public enum StoreItemCode
{
    /// <summary>
    /// Grants the user 10 GB of upload credit.
    /// </summary>
    UploadCredit10GB,

    /// <summary>
    /// Grants the user 50 GB of upload credit.
    /// </summary>
    UploadCredit50GB,

    /// <summary>
    /// Grants the user a single invitation code.
    /// </summary>
    InviteOne,

    /// <summary>
    /// Grants the user five invitation codes.
    /// </summary>
    InviteFive,

    /// <summary>
    /// Activates double upload status for a period.
    /// </summary>
    DoubleUpload,

    /// <summary>
    /// Activates no Hit & Run status for a period.
    /// </summary>
    NoHitAndRun,

    /// <summary>
    /// Purchases a specific badge.
    /// </summary>
    Badge
}
