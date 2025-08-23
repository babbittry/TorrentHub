using TorrentHub.Enums;

namespace TorrentHub.DTOs
{
    public class UpdateUserAdminDto
    {
        public UserRole? Role { get; set; }
        public bool? IsBanned { get; set; }
        public UserBanReason? BanReason { get; set; }
        public DateTimeOffset? BanUntil { get; set; }
    }
}
