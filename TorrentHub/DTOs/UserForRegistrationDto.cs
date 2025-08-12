using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UserForRegistrationDto
{
    [Required]
    public required string UserName { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string InviteCode { get; set; }
}
