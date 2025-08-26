
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreatePollDto
{
    [Required]
    [StringLength(255, MinimumLength = 5)]
    public required string Question { get; set; }

    [Required]
    [MinLength(2, ErrorMessage = "A poll must have at least two options.")]
    public required List<string> Options { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }
}
