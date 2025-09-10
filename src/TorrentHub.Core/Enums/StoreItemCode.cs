namespace TorrentHub.Core.Enums;

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
    /// Grants the user 100 GB of upload credit.
    /// </summary>
    UploadCredit100GB,

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
    /// Allows the user to change their username.
    /// </summary>
    ChangeUsername,

    /// <summary>
    /// Purchases a specific badge.
    /// </summary>
    Badge,

    /// <summary>
    /// Allows the user to set or change their short signature.
    /// </summary>
    ShortSignature,

    /// <summary>
    /// Activates a colorful username effect for a period.
    /// </summary>
    ColorfulUsername,
}
