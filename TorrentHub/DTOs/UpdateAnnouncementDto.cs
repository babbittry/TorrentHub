
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateAnnouncementDto
{
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public required string Title { get; set; }

    [Required]
    [StringLength(10000, MinimumLength = 10)]
    public required string Content { get; set; }
}
