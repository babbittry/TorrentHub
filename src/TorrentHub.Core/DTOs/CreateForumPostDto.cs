using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreateForumPostDto
{
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }

    // Reply functionality fields
    public int? ParentPostId { get; set; }
    public int? ReplyToUserId { get; set; }
}
