using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UserForLogin2faDto
{
    [Required]
    public required string UserName { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string Code { get; set; }
}