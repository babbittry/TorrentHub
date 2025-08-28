using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UpdateCoinsRequestDto
{
    [Required]
    public ulong Amount { get; set; }
}
