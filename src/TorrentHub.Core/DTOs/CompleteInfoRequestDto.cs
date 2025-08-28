using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CompleteInfoRequestDto
{
    [Required]
    public required string ImdbId { get; set; }
}
