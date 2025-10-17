using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs
{
    public class UpdateTorrentCommentRequestDto
    {
        [Required]
        [StringLength(500)]
        public required string Content { get; set; }
    }
}