
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class VoteDto
{
    [Required]
    public required string Option { get; set; }
}
