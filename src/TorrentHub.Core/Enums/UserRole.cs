namespace TorrentHub.Core.Enums;

public enum UserRole
{
    // Standard User Tiers (increasing privileges)
    Mosquito = 0,   // 低分享率用户
    User,           // 普通用户 (新注册用户的默认角色)
    PowerUser,      // 高级用户
    EliteUser,      // 精英用户
    CrazyUser,      // 狂热用户
    VeteranUser,    // 资深用户
    VIP,            // VIP用户（例如，捐赠者或特殊贡献者）

    // Functional Roles
    Uploader,       // 认证上传者
    Seeder,         // 保种用户
    

    // Staff Roles
    Moderator,      // 版主
    Administrator   // 管理员
}
