namespace TorrentHub.DTOs;

public class AnnouncementDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserPublicProfileDto? CreatedByUser { get; set; }
}
