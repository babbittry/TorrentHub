using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sakura.PT.Entities;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Text { get; set; }

    [Required]
    public int TorrentId { get; set; }

    [ForeignKey(nameof(TorrentId))]
    public Torrent? Torrent { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
