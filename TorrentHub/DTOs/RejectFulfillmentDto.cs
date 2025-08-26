
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class RejectFulfillmentDto
{
    [Required]
    [StringLength(500, ErrorMessage = "Rejection reason cannot be longer than 500 characters.")]
    public required string Reason { get; set; }
}
