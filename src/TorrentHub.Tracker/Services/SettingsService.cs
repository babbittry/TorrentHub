using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;

namespace TorrentHub.Tracker.Services;

/// <summary>
/// A lightweight settings service for the Tracker that reads settings directly from the database/cache.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string SiteSettingsKey = "SiteSettings";
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Cache settings for 5 minutes
    };

    public SettingsService(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<SiteSettingsDto> GetSiteSettingsAsync()
    {
        var cachedSettings = await _cache.GetStringAsync(SiteSettingsKey);
        if (cachedSettings != null)
        {
            return JsonSerializer.Deserialize<SiteSettingsDto>(cachedSettings) ?? new SiteSettingsDto();
        }

        var setting = await _context.SiteSettings.FindAsync(SiteSettingsKey);
        if (setting != null && !string.IsNullOrEmpty(setting.Value))
        {
            var dto = JsonSerializer.Deserialize<SiteSettingsDto>(setting.Value) ?? new SiteSettingsDto();
            await _cache.SetStringAsync(SiteSettingsKey, setting.Value, _cacheOptions);
            return dto;
        }

        // Return default settings if not found in DB
        return new SiteSettingsDto();
    }

    public Task UpdateSiteSettingsAsync(SiteSettingsDto dto)
    {
        // This is a read-only implementation for the tracker.
        // The Web project is responsible for writing settings.
        throw new NotImplementedException("Tracker service cannot update site settings.");
    }
}
