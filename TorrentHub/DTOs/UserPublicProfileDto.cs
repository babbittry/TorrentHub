using TorrentHub.Enums;

namespace TorrentHub.DTOs;

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
   public DateTime CreatedAt { get; set; }
   public ulong Coins { get; set; }
   public bool IsDoubleUploadActive { get; set; }
   public DateTime? DoubleUploadExpiresAt { get; set; }
   public bool IsNoHRActive { get; set; }
   public DateTime? NoHRExpiresAt { get; set; }
   public ulong TotalSeedingTimeMinutes { get; set; }
   public ulong TotalLeechingTimeMinutes { get; set; } 
   public uint InviteNum { get; set; }
}
