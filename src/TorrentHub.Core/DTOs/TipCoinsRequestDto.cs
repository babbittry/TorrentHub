using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class TipCoinsRequestDto
{
    [Required]
    public int ToUserId { get; set; }

    [Required]
    [Range(1, ulong.MaxValue)]
    public ulong Amount { get; set; }

    [Required]
    [StringLength(100)]
    public string ContextType { get; set; } = string.Empty; // e.g., "Torrent", "Comment", "ForumPost"

    [Required]
    public int ContextId { get; set; }
}