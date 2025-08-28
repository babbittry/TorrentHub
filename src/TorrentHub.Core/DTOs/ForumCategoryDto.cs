using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class ForumCategoryDto
{
    public int Id { get; set; }
    public ForumCategoryCode Code { get; set; }
    public int DisplayOrder { get; set; }
    public int TopicCount { get; set; }
    public int PostCount { get; set; }
}

