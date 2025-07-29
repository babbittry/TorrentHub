using System.ComponentModel.DataAnnotations;
using Sakura.PT.Enums;

namespace Sakura.PT.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public required string UserName { get; set; }
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string PasswordHash { get; set; }
    public string? Avatar { get; set; }
    public string Language { get; set; } = "zh-CN";
    [Required]
    public long UploadedBytes { get; set; }
    [Required]
    public long DownloadedBytes { get; set; }
    public string RssKey {get; set;}
    [Required]
    public UserRole Role { get; set; } = UserRole.NotConfirmEmail;
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsBanned { get; set; } = false;
    public int InviteNum { get; set; } = 0;
    public int InviteById { get; set; }
}