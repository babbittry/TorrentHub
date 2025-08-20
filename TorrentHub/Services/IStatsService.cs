using TorrentHub.DTOs;

namespace TorrentHub.Services;

public interface IStatsService
{
    Task<SiteStatsDto> GetSiteStatsAsync();
    Task<SiteStatsDto> RecalculateAndCacheSiteStatsAsync();
}
