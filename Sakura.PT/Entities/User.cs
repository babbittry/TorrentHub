using System.ComponentModel.DataAnnotations;
using Sakura.PT.Enums;

namespace Sakura.PT.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string UserName { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public string PasswordHash { get; set; }
    [Required]
    public long UploadedBytes { get; set; }
    [Required]
    public long DownloadedBytes { get; set; }
    public string PassKey {get; set;}
    [Required]
    public UserRole Role { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}