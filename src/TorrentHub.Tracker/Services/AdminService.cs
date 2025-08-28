using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;

namespace TorrentHub.Tracker.Services;

/// <summary>
/// A lightweight admin service for the Tracker.
/// It only implements the methods required by the Announce process.
/// </summary>
public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string BannedClientsCacheKey = "BannedClients";
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public AdminService(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<BannedClient>> GetBannedClientsAsync()
    {
        var cachedClients = await _cache.GetStringAsync(BannedClientsCacheKey);
        if (cachedClients != null)
        {
            return JsonSerializer.Deserialize<List<BannedClient>>(cachedClients) ?? new List<BannedClient>();
        }

        var clients = await _context.BannedClients.ToListAsync();
        await _cache.SetStringAsync(BannedClientsCacheKey, JsonSerializer.Serialize(clients), _cacheOptions);
        return clients;
    }

    public async Task LogCheatAsync(int userId, string reason, string details)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        user.CheatWarningCount++;

        var log = new CheatLog
        {
            UserId = userId,
            Reason = reason,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow
        };

        _context.CheatLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    // The following methods are not needed by the Tracker service.
    public Task<BannedClient> AddBannedClientAsync(BannedClientDto dto) => throw new NotImplementedException();
    public Task<(bool Success, string Message)> DeleteBannedClientAsync(int id) => throw new NotImplementedException();
    public Task<List<DuplicateIpUserDto>> GetDuplicateIpUsersAsync() => throw new NotImplementedException();
    public Task<List<CheatLogDto>> GetCheatLogsAsync() => throw new NotImplementedException();
    public Task<List<JsonDocument>> SearchSystemLogsAsync(LogSearchDto dto) => throw new NotImplementedException();
    public Task<PaginatedResult<UserProfileDetailDto>> GetUsersAsync(int page, int pageSize) => throw new NotImplementedException();
}
