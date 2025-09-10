using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents the private profile information for a user.
/// This DTO contains more sensitive information than the public profile,
/// and should only be sent to the authenticated user themselves.
/// </summary>
public class UserPrivateProfileDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public BanStatus BanStatus { get; set; }
    public UserRole Role { get; set; }
    public ulong Coins { get; set; }
    public uint InviteNum { get; set; }
    public string? Avatar { get; set; }
    public string? Signature { get; set; }
    public required string Language { get; set; }
    public ulong UploadedBytes { get; set; }
    public ulong DownloadedBytes { get; set; }
    public ulong NominalUploadedBytes { get; set; }
    public ulong NominalDownloadedBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? BanReason { get; set; }
    public int CheatWarningCount { get; set; }
    public DateTimeOffset? BanUntil { get; set; }
    public ulong TotalSeedingTimeMinutes { get; set; }
    public ulong TotalLeechingTimeMinutes { get; set; }
    public bool IsDoubleUploadActive { get; set; }
    public DateTimeOffset? DoubleUploadExpiresAt { get; set; }
    public bool IsNoHRActive { get; set; }
    public DateTimeOffset? NoHRExpiresAt { get; set; }

    /// <summary>
    /// The two-factor authentication method currently enabled by the user.
    /// Expected values: "Email", "AuthenticatorApp".
    /// </summary>
    public required string TwoFactorMethod { get; set; }
    public string? UserTitle { get; set; }
    public int? EquippedBadgeId { get; set; }
    public DateTimeOffset? ColorfulUsernameExpiresAt { get; set; }
}
