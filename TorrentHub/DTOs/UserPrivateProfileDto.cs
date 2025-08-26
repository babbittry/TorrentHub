using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class UserPrivateProfileDto : UserPublicProfileDto
{
    public required string Email { get; set; }
    public BanStatus BanStatus { get; set; }
    public string? BanReason { get; set; }
    public DateTimeOffset? BanUntil { get; set; }
    public string? Language { get; set; }
    public int CheatWarningCount { get; set; }
}