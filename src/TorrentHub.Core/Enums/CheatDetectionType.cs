namespace TorrentHub.Core.Enums;

/// <summary>
/// Represents the type of cheat detected.
/// </summary>
public enum CheatDetectionType
{
    /// <summary>
    /// User is announcing too frequently.
    /// </summary>
    AnnounceSpam = 1,

    /// <summary>
    /// User is reporting unrealistic speeds.
    /// </summary>
    SpeedCheat = 2,

    /// <summary>
    /// User is announcing from multiple locations simultaneously.
    /// </summary>
    MultiLocation = 3,

    /// <summary>
    /// User is using a forged or banned client.
    /// </summary>
    ClientSpoof = 4,
}