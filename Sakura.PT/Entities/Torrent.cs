using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sakura.PT.Enums;

namespace Sakura.PT.Entities;

public class Torrent
{
    [Key]
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    public string InfoHash { get; set; }
    public string FilePath { get; set; }
    public string Description { get; set; }
    [Required]
    public int UploadedByUserId { get; set; }
    [ForeignKey(nameof(UploadedByUserId))]
    public User UploadedByUser { get; set; }
    [Required]
    public TorrentCategory Category { get; set; }
    [Required]
    public long Size { get; set; }
    [Required]
    public Boolean IsDeleted { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}