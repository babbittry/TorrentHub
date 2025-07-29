using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.Entities;

public class Peers
{
    [Key]
    public int Id { get; set; }
    public int TorrentId { get; set; }
    public int UserId { get; set; }
    [Required]
    public string IpAddress { get; set; }
    [Required]
    public int Port { get; set; }
    public DateTime LastAnnounce { get; set; } = DateTime.UtcNow;
}