using System.ComponentModel.DataAnnotations;
using TorrentHub.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a category or section in the forum.
/// </summary>
public class ForumCategory
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "forum_category_code")]
    public required ForumCategoryCode Code { get; set; }

    [Required]
    public int DisplayOrder { get; set; } = 0;
    
    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}

