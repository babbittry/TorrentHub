using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CompleteInfoRequestDto
{
    [Required]
    public required string ImdbId { get; set; }
}
