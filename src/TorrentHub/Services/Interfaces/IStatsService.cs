using TorrentHub.Core.DTOs;

namespace TorrentHub.Services.Interfaces;

public interface IStatsService
{
    Task<SiteStatsDto> GetSiteStatsAsync();
    Task<SiteStatsDto> RecalculateAndCacheSiteStatsAsync();
}

