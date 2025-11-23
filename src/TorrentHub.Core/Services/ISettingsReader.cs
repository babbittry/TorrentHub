using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Services;

/// <summary>
/// 配置读取服务接口
/// 用于只需要读取配置的场景(如Tracker)
/// </summary>
public interface ISettingsReader
{
    /// <summary>
    /// 获取完整的站点配置
    /// </summary>
    Task<SiteSettingsDto> GetSiteSettingsAsync();
}