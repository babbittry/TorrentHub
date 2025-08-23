using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class UserPrivateProfileDto : UserPublicProfileDto
{
    public required string Email { get; set; }
    public UserBanReason? BanReason { get; set; }
    public DateTimeOffset? BanUntil { get; set; }
}