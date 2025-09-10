using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UpdateUserTitleRequestDto
{
    [Required]
    [StringLength(30)]
    public required string Title { get; set; }
}