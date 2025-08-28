using TorrentHub.Core.DTOs;

namespace TorrentHub.Services;

public interface IStatsService
{
    Task<SiteStatsDto> GetSiteStatsAsync();
    Task<SiteStatsDto> RecalculateAndCacheSiteStatsAsync();
}

