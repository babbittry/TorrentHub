using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;

namespace TorrentHub.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AdminService> _logger;
    private const string BannedClientsCacheKey = "BannedClients";
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public AdminService(ApplicationDbContext context, IDistributedCache cache, ILogger<AdminService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
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

    public async Task<BannedClient> AddBannedClientAsync(BannedClientDto dto)
    {
        var existing = await _context.BannedClients
            .FirstOrDefaultAsync(c => c.UserAgentPrefix == dto.UserAgentPrefix);

        if (existing != null)
        {
            throw new Exception("This client prefix is already banned.");
        }

        var bannedClient = new BannedClient
        {
            UserAgentPrefix = dto.UserAgentPrefix,
            Reason = dto.Reason
        };

        _context.BannedClients.Add(bannedClient);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(BannedClientsCacheKey);

        return bannedClient;
    }

    public async Task<(bool Success, string Message)> DeleteBannedClientAsync(int id)
    {
        var bannedClient = await _context.BannedClients.FindAsync(id);
        if (bannedClient == null)
        {
            return (false, "error.bannedClient.notFound");
        }

        _context.BannedClients.Remove(bannedClient);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(BannedClientsCacheKey);

        return (true, "bannedClient.delete.success");
    }

    public async Task<List<DuplicateIpUserDto>> GetDuplicateIpUsersAsync()
    {
        var duplicateIps = await _context.Peers
            .AsNoTracking()
            .GroupBy(p => p.IpAddress)
            .Where(g => g.Select(p => p.UserId).Distinct().Count() > 1)
            .Select(g => new DuplicateIpUserDto
            {
                Ip = g.Key.ToString(),
                Users = g.Select(p => p.User).Distinct().Select(u => new UserSummaryDto { Id = u.Id, UserName = u.UserName }).ToList()
            })
            .ToListAsync();

        return duplicateIps;
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

    public async Task<List<CheatLogDto>> GetCheatLogsAsync()
    {
        return await _context.CheatLogs
            .AsNoTracking()
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new CheatLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User == null ? null : l.User.UserName,
                Timestamp = l.Timestamp,
                Reason = l.Reason,
                Details = l.Details
            })
            .ToListAsync();
    }

    public async Task<List<JsonDocument>> SearchSystemLogsAsync(LogSearchDto dto)
    {
        var logFilePath = Path.Combine("logs", $"log-{DateTime.UtcNow:yyyyMMdd}.json");
        if (!File.Exists(logFilePath))
        {
            return new List<JsonDocument>();
        }

        var results = new List<JsonDocument>();
        using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var logEntry = JsonDocument.Parse(line);

                if (!string.IsNullOrEmpty(dto.Level) && 
                    (!logEntry.RootElement.TryGetProperty("Level", out var levelElement) || 
                     !levelElement.GetString()!.Equals(dto.Level, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(dto.Query) && 
                    (!logEntry.RootElement.TryGetProperty("RenderedMessage", out var msgElement) || 
                     !msgElement.GetString()!.Contains(dto.Query, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                results.Add(logEntry);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse log entry: {LogLine}", line);
            }
        }

        return results.OrderByDescending(j => j.RootElement.GetProperty("Timestamp").GetDateTimeOffset()).Skip(dto.Offset).Take(dto.Limit).ToList();
    }

    public async Task<PaginatedResult<UserProfileDetailDto>> GetUsersAsync(int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking()
            .Include(u => u.Invite)
            .ThenInclude(i => i.GeneratorUser);

        var totalItems = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserProfileDetailDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role.ToString(),
                Avatar = u.Avatar,
                CreatedAt = u.CreatedAt,
                UploadedBytes = u.UploadedBytes,
                DownloadedBytes = u.DownloadedBytes,
                NominalUploadedBytes = u.NominalUploadedBytes,
                NominalDownloadedBytes = u.NominalDownloadedBytes,
                Coins = u.Coins,
                TotalSeedingTimeMinutes = u.TotalSeedingTimeMinutes,
                TotalLeechingTimeMinutes = u.TotalLeechingTimeMinutes,
                InvitedBy = u.Invite == null ? null : u.Invite.GeneratorUser!.UserName,
                // SeedingSize = u.SeedingSize, // TODO: Calculate this
                // CurrentSeedingCount = u.SeedingCount, // TODO: Calculate this
                // CurrentLeechingCount = u.LeechingCount // TODO: Calculate this
            })
            .ToListAsync();

        return new PaginatedResult<UserProfileDetailDto>
        {
            Items = users,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }
}

