using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UserForLoginDto
{
    [Required]
    public required string UserName { get; set; }

    [Required]
    public required string Password { get; set; }
}
