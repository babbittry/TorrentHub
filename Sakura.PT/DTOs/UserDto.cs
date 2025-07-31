using Sakura.PT.Enums;

namespace Sakura.PT.DTOs;

public class UserDto
{
   public int Id { get; set; }
   public required string UserName { get; set; }
   public string? Avatar { get; set; }
   public long UploadedBytes { get; set; }
   public long DownloadedBytes { get; set; }
   public UserRole Role { get; set; }
   public DateTime CreatedAt { get; set; }
   public long SakuraCoins { get; set; }
   public bool IsDoubleUploadActive { get; set; }
   public DateTime? DoubleUploadExpiresAt { get; set; }
   public bool IsNoHRActive { get; set; }
   public DateTime? NoHRExpiresAt { get; set; }
}