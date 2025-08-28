
namespace TorrentHub.Core.DTOs;

public class PollDto
{
    public int Id { get; set; }
    public required string Question { get; set; }
    public required Dictionary<string, int> Results { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int TotalVotes { get; set; }
    public string? UserVotedOption { get; set; }
    public bool IsActive => DateTimeOffset.UtcNow < ExpiresAt;
}
