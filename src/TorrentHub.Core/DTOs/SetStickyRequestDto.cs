using System.ComponentModel.DataAnnotations;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class SetStickyRequestDto
{
    [Required]
    public TorrentStickyStatus Status { get; set; }
}

