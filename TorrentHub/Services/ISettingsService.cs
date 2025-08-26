
using TorrentHub.DTOs;

namespace TorrentHub.Services;

public interface ISettingsService
{
    Task<SiteSettingsDto> GetSiteSettingsAsync();
    Task UpdateSiteSettingsAsync(SiteSettingsDto dto);
}

