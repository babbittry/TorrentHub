using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class SendEmailCodeRequestDto
{
    [Required]
    public required string UserName { get; set; }
}