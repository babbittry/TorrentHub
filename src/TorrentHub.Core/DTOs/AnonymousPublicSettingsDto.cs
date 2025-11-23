namespace TorrentHub.Core.DTOs;

/// <summary>
/// 匿名用户可访问的公开配置
/// 无需认证即可获取，用于注册页、登录页、页面布局等场景
/// </summary>
public class AnonymousPublicSettingsDto
{
    /// <summary>
    /// 站点名称
    /// </summary>
    public string SiteName { get; set; } = "TorrentHub";

    /// <summary>
    /// 站点Logo URL
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 是否开放注册
    /// </summary>
    public bool IsRegistrationOpen { get; set; } = false;

    /// <summary>
    /// 是否启用论坛功能
    /// </summary>
    public bool IsForumEnabled { get; set; } = true;
}