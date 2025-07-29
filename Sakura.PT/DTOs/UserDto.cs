using Sakura.PT.Enums;

namespace Sakura.PT.DTOs;

public class UserDto
{
   public int Id { get; set; }
   public string Username { get; set; }
   public string? Avatar { get; set; }
   public long UploadedBytes { get; set; }
   public long DownloadedBytes { get; set; }
   public UserRole Role { get; set; }
   public DateTime CreatedAt { get; set; }
}