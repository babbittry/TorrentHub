
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.Entities;

public class BannedClient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string UserAgentPrefix { get; set; }

    [StringLength(255)]
    public string? Reason { get; set; }
}
