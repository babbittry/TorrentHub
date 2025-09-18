
using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Services;

public interface ISettingsService
{
    Task<SiteSettingsDto> GetSiteSettingsAsync();
    Task UpdateSiteSettingsAsync(SiteSettingsDto dto);
    Task<PublicSiteSettingsDto> GetPublicSiteSettingsAsync();
}


