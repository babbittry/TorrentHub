using Sakura.PT.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class UploadTorrentRequestDto
{
    public string? Description { get; set; }

    [Required]
    public TorrentCategory Category { get; set; }
}
