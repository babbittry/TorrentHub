namespace TorrentHub.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public UserPublicProfileDto? Sender { get; set; }
    public UserPublicProfileDto? Receiver { get; set; }
    public required string Subject { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public bool IsRead { get; set; }
}
