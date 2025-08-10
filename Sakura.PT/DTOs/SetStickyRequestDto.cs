using Sakura.PT.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class SetStickyRequestDto
{
    [Required]
    public TorrentStickyStatus Status { get; set; }
}
