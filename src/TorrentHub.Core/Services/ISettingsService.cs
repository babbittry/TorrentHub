namespace TorrentHub.Core.Services;

/// <summary>
/// 配置服务接口 (组合了读写接口)
/// Web项目实现此接口以提供完整的配置管理功能
/// </summary>
public interface ISettingsService : ISettingsReader, ISettingsWriter
{
}


