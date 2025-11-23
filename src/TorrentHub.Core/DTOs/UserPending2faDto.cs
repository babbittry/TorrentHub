namespace TorrentHub.Core.DTOs;

/// <summary>
/// 用户待 2FA 验证状态的精简信息
/// 仅在登录第一阶段（用户名密码验证成功，等待 2FA）时返回
/// 不包含敏感信息如金币、上传下载量等
/// </summary>
public class UserPending2faDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// 脱敏后的邮箱地址
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 两步验证方式
    /// 前端根据此字段决定显示哪种验证码输入界面
    /// 可能的值: "Email", "AuthenticatorApp"
    /// </summary>
    public required string TwoFactorMethod { get; set; }
}