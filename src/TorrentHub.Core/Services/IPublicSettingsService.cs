using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Services;

/// <summary>
/// 公开配置服务接口
/// 仅Web项目需要实现,用于向前端提供公开配置API
/// </summary>
public interface IPublicSettingsService
{
    /// <summary>
    /// 获取匿名用户可访问的公开配置（无需认证）
    /// </summary>
    Task<AnonymousPublicSettingsDto> GetAnonymousPublicSettingsAsync();
    
    /// <summary>
    /// 获取认证用户可访问的完整公开配置（需要认证）
    /// </summary>
    Task<PublicSiteSettingsDto> GetPublicSiteSettingsAsync();
}