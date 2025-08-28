
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class VoteDto
{
    [Required]
    public required string Option { get; set; }
}
