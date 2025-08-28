
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class BannedClientDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string UserAgentPrefix { get; set; }

    [StringLength(255)]
    public string? Reason { get; set; }
}
