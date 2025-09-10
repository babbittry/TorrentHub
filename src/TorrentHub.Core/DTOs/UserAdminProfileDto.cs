using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents user profile information tailored for administrators.
/// Includes all public fields plus sensitive information like email.
/// </summary>
public class UserAdminProfileDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? Avatar { get; set; }
    public string? Signature { get; set; }
    public ulong UploadedBytes { get; set; }
    public ulong DownloadedBytes { get; set; }
    public ulong NominalUploadedBytes { get; set; }
    public ulong NominalDownloadedBytes { get; set; }
    public UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ulong Coins { get; set; }
    public bool IsDoubleUploadActive { get; set; }
    public DateTimeOffset? DoubleUploadExpiresAt { get; set; }
    public bool IsNoHRActive { get; set; }
    public DateTimeOffset? NoHRExpiresAt { get; set; }
    public ulong TotalSeedingTimeMinutes { get; set; }
    public ulong TotalLeechingTimeMinutes { get; set; }
    public uint InviteNum { get; set; }
    public BanStatus BanStatus { get; set; }
    public string? BanReason { get; set; }
    public DateTimeOffset? BanUntil { get; set; }

    // --- Detail Fields ---
    public string? InvitedBy { get; set; }
    public ulong SeedingSize { get; set; }
    public int CurrentSeedingCount { get; set; }
    public int CurrentLeechingCount { get; set; }
}