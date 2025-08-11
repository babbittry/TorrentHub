using Sakura.PT.Enums;

namespace Sakura.PT.DTOs;

public class UserPrivateProfileDto : UserPublicProfileDto
{
    public required string Email { get; set; }
    public UserBanReason? BanReason { get; set; }
    public DateTime? BanUntil { get; set; }
}