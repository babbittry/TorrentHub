namespace TorrentHub.DTOs;

/// <summary>
/// Contains detailed information for a user's profile page.
/// </summary>
public class UserProfileDetailDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string? Avatar { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ulong UploadedBytes { get; set; }
    public ulong DownloadedBytes { get; set; }
    public ulong NominalUploadedBytes { get; set; }
    public ulong NominalDownloadedBytes { get; set; }
    public ulong Coins { get; set; }
    public ulong TotalSeedingTimeMinutes { get; set; }
    public ulong TotalLeechingTimeMinutes { get; set; }

    // New fields
    public string? InvitedBy { get; set; }
    public ulong SeedingSize { get; set; }
    public int CurrentSeedingCount { get; set; }
    public int CurrentLeechingCount { get; set; }
}
