using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class TwoFactorVerificationRequestDto
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string Code { get; set; }
}