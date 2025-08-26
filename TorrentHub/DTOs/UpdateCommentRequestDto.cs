using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs
{
    public class UpdateCommentRequestDto
    {
        [Required]
        [StringLength(500)]
        public required string Content { get; set; }
    }
}