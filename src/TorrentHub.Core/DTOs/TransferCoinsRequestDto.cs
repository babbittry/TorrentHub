using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class TransferCoinsRequestDto
{
    [Required]
    public int ToUserId { get; set; }

    [Required]
    [Range(1, ulong.MaxValue)]
    public ulong Amount { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }
}