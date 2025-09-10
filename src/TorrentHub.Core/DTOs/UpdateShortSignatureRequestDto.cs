using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UpdateShortSignatureRequestDto
{
    [Required]
    [StringLength(30)]
    public required string Signature { get; set; }
}