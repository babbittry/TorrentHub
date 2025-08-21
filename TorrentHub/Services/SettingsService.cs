using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Data;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string SettingsCacheKeyPrefix = "Settings:";
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        // Keep settings in cache for a long time, as they don't change often.
        // They are invalidated upon update.
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
    };

    public SettingsService(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var cacheKey = $"{SettingsCacheKeyPrefix}{key}";
        var cachedValue = await _cache.GetStringAsync(cacheKey);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var setting = await _context.SiteSettings.FindAsync(key);
        var value = setting?.Value;

        if (value != null)
        {
            await _cache.SetStringAsync(cacheKey, value, _cacheOptions);
        }

        return value;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var setting = await _context.SiteSettings.FindAsync(key);

        if (setting == null)
        {
            setting = new SiteSetting { Key = key, Value = value };
            _context.SiteSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            _context.SiteSettings.Update(setting);
        }

        await _context.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = $"{SettingsCacheKeyPrefix}{key}";
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task<bool> IsRegistrationOpenAsync()
    {
        var value = await GetSettingAsync("IsRegistrationOpen");
        return bool.TryParse(value, out var result) && result;
    }
}
