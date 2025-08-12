using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateCoinsRequestDto
{
    [Required]
    public ulong Amount { get; set; }
}
