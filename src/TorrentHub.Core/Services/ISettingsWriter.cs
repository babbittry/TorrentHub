using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Services;

/// <summary>
/// 配置写入服务接口
/// 用于需要修改配置的场景(仅Web管理功能)
/// </summary>
public interface ISettingsWriter
{
    /// <summary>
    /// 更新站点配置
    /// </summary>
    Task UpdateSiteSettingsAsync(SiteSettingsDto dto);
}