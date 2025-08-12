using System.ComponentModel.DataAnnotations;
using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class UploadTorrentRequestDto
{
    public string? Description { get; set; }

    [Required]
    public TorrentCategory Category { get; set; }
}
