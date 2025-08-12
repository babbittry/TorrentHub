using System.ComponentModel.DataAnnotations;
using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class SetStickyRequestDto
{
    [Required]
    public TorrentStickyStatus Status { get; set; }
}
