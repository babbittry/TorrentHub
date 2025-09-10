using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class UserPublicProfileDto
{
   public int Id { get; set; }
   public required string UserName { get; set; }
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

   // --- Fields from UserProfileDetailDto ---
   public string? InvitedBy { get; set; }
   public ulong SeedingSize { get; set; }
   public int CurrentSeedingCount { get; set; }
   public int CurrentLeechingCount { get; set; }
   public string? ShortSignature { get; set; }
   public int? EquippedBadgeId { get; set; }
   public DateTimeOffset? ColorfulUsernameExpiresAt { get; set; }
}
