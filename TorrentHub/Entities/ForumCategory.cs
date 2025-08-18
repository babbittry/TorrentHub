using System.ComponentModel.DataAnnotations;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a category or section in the forum.
/// </summary>
public class ForumCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ForumCategoryCode Code { get; set; }

    [Required]
    public int DisplayOrder { get; set; } = 0;
    
    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}
