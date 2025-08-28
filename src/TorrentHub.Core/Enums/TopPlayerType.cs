namespace TorrentHub.Core.Enums;

/// <summary>
/// Represents different types of top player rankings.
/// </summary>
public enum TopPlayerType
{
    Uploaded = 0,       // Top users by total uploaded bytes
    Downloaded = 1,     // Top users by total downloaded bytes
    Coins = 2,    // Top users by Coin balance
    SeedingTime = 3,    // Top users by total seeding time
    SeedingSize = 4     // Top users by total seeding size
}
