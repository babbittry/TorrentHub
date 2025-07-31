namespace Sakura.PT.Enums;

/// <summary>
/// Represents different types of top player rankings.
/// </summary>
public enum TopPlayerType
{
    Uploaded,       // Top users by total uploaded bytes
    Downloaded,     // Top users by total downloaded bytes
    SakuraCoins,    // Top users by SakuraCoin balance
    SeedingTime,    // Top users by total seeding time
    SeedingSize     // Top users by total seeding size
}
