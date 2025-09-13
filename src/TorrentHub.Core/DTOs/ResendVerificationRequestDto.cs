using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class ResendVerificationRequestDto
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;
}