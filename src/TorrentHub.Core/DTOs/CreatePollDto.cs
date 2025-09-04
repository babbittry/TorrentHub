
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreatePollDto
{
    [Required]
    [MaxLength(255)]
    public required string Question { get; set; }

    [Required]
    [MinLength(2, ErrorMessage = "A poll must have at least two options.")]
    public required List<string> Options { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }
}
