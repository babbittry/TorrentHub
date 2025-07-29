namespace Sakura.PT.Enums;

    public enum UserRole
{
    // Unprivileged/Special States
    Guest = 0,          // 未登录或访客用户
    Validating,         // 已注册但未激活（例如，未验证邮箱）
    Banned,             // 被封禁用户
    Disabled,           // 账户被禁用（例如，管理员禁用或用户自禁用）
    Pruned,             // 账户被清除/修剪（例如，长期不活跃）
    Mosquito,           // 吸血（低分享率用户）

    // Standard User Tiers (increasing privileges)
    User,               // 普通用户
    PowerUser,          // 高级用户
    EliteUser,          // 精英用户
    VeteranUser,        // 资深用户
    VIP,                // VIP用户（例如，捐赠者或特殊贡献者）

    // Functional Roles
    Seeder,             // 保种用户
    Uploader,           // 上传者
    Archivist,          // 档案管理员（管理旧内容、维护）

    // Staff Roles
    Moderator,          // 版主
    Administrator,      // 管理员
    Developer,          // 开发者（拥有比管理员更高的系统访问权限）
    Owner,              // 站长/所有者（最高权限）

    // System Roles
    Bot,                // 机器人账户
    Internal            // 内部系统操作账户（例如，API密钥）
}