using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;

namespace TorrentHub.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string SiteSettingsKey = "SiteSettings";
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) // Keep settings in cache for a long time
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

    public async Task UpdateSiteSettingsAsync(SiteSettingsDto dto)
    {
        var setting = await _context.SiteSettings.FindAsync(SiteSettingsKey);
        var value = JsonSerializer.Serialize(dto);

        if (setting == null)
        {
            setting = new SiteSetting { Key = SiteSettingsKey, Value = value };
            _context.SiteSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            _context.SiteSettings.Update(setting);
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(SiteSettingsKey);
    }
}
