using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class UpdateUserAdminDto
{
    public UserRole? Role { get; set; }
    public BanStatus? BanStatus { get; set; }
    public string? BanReason { get; set; }
    public DateTimeOffset? BanUntil { get; set; }
    public int? CheatWarningCount { get; set; }
    public uint? InviteNum { get; set; }
    public ulong? Coins { get; set; }
    public ulong? TotalSeedingTimeMinutes { get; set; }
    public ulong? TotalLeechingTimeMinutes { get; set; }
    public bool? IsDoubleUploadActive { get; set; }
    public DateTimeOffset? DoubleUploadExpiresAt { get; set; }
    public bool? IsNoHRActive { get; set; }
    public DateTimeOffset? NoHRExpiresAt { get; set; }
    public ulong? NominalUploadedBytes { get; set; }
    public ulong? NominalDownloadedBytes { get; set; }
}