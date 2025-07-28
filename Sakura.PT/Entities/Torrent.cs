using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.Entities;

public class Torrent
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string InfoHash { get; set; }
    public string Description { get; set; }
    [Required]
    public long Size { get; set; }
    public int UploadedByUserId { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}