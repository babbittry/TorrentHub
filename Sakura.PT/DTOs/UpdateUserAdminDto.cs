using Sakura.PT.Enums;

namespace Sakura.PT.DTOs
{
    public class UpdateUserAdminDto
    {
        public UserRole? Role { get; set; }
        public bool? IsBanned { get; set; }
        public UserBanReason? BanReason { get; set; }
        public DateTime? BanUntil { get; set; }
    }
}
