
namespace TorrentHub.DTOs;

public class CheatLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public required string Reason { get; set; }
    public string? Details { get; set; }
}
