using Sakura.PT.Enums;

namespace Sakura.PT.DTOs;

public class UserPublicProfileDto
{
   public int Id { get; set; }
   public required string UserName { get; set; }
   public string? Avatar { get; set; }
   public string? Signature { get; set; }
   public ulong UploadedBytes { get; set; }
   public ulong DownloadedBytes { get; set; }
   public UserRole Role { get; set; }
   public DateTime CreatedAt { get; set; }
   public ulong SakuraCoins { get; set; }
   public bool IsDoubleUploadActive { get; set; }
   public DateTime? DoubleUploadExpiresAt { get; set; }
   public bool IsNoHRActive { get; set; }
   public DateTime? NoHRExpiresAt { get; set; }
   public ulong TotalSeedingTimeMinutes { get; set; }
   public uint InviteNum { get; set; }
}