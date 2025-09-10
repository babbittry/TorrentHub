using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UserForRegistrationDto
{
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public required string UserName { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    public string? InviteCode { get; set; }

    [Required]
    public required string AvatarSvg { get; set; }

    public string? Language { get; set; }
}
