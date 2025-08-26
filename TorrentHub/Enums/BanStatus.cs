
namespace TorrentHub.Enums;

[Flags]
public enum BanStatus
{
    None = 0,
    LoginBan = 1 << 0,       // 1
    TrackerBan = 1 << 1,     // 2
    DownloadBan = 1 << 2,    // 4
    ForumBan = 1 << 3,       // 8
    MessagingBan = 1 << 4,   // 16
    InviteBan = 1 << 5       // 32
}
