using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

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

    public async Task LogCheatAsync(
        int userId,
        CheatDetectionType detectionType,
        CheatSeverity severity,
        string? details = null,
        int? torrentId = null,
        string? ipAddress = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        user.CheatWarningCount++;

        var log = new CheatLog
        {
            UserId = userId,
            DetectionType = detectionType,
            Severity = severity,
            Details = details,
            TorrentId = torrentId,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };

        _context.CheatLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResult<CheatLogDto>> GetCheatLogsAsync(
        int page = 1,
        int pageSize = 50,
        int? userId = null,
        string? detectionType = null)
    {
        var query = _context.CheatLogs
            .AsNoTracking()
            .Include(l => l.User)
            .Include(l => l.Torrent)
            .AsQueryable();

        // 应用过滤
        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (!string.IsNullOrEmpty(detectionType) && Enum.TryParse<CheatDetectionType>(detectionType, true, out var detectionTypeEnum))
            query = query.Where(l => l.DetectionType == detectionTypeEnum);

        query = query.OrderByDescending(l => l.Timestamp);

        var totalItems = await query.CountAsync();

        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new CheatLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User == null ? null : l.User.UserName,
                TorrentId = l.TorrentId,
                TorrentName = l.Torrent == null ? null : l.Torrent.Name,
                DetectionType = l.DetectionType,
                Severity = l.Severity,
                IpAddress = l.IpAddress,
                Timestamp = l.Timestamp,
                Details = l.Details,
                IsProcessed = l.IsProcessed,
                ProcessedAt = l.ProcessedAt,
                ProcessedByUserId = l.ProcessedByUserId,
                ProcessedByUsername = l.ProcessedByUser == null ? null : l.ProcessedByUser.UserName,
                AdminNotes = l.AdminNotes
            })
            .ToListAsync();

        return new PaginatedResult<CheatLogDto>
        {
            Items = logs,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
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

    public async Task<PaginatedResult<UserAdminProfileDto>> GetUsersAsync(int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking()
            .Include(u => u.Invite)
            .ThenInclude(i => i!.GeneratorUser);

        var totalItems = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserAdminProfileDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role,
                Avatar = u.Avatar,
                Signature = u.Signature,
                CreatedAt = u.CreatedAt,
                UploadedBytes = u.UploadedBytes,
                DownloadedBytes = u.DownloadedBytes,
                NominalUploadedBytes = u.NominalUploadedBytes,
                NominalDownloadedBytes = u.NominalDownloadedBytes,
                Coins = u.Coins,
                InviteNum = u.InviteNum,
                IsDoubleUploadActive = u.IsDoubleUploadActive,
                DoubleUploadExpiresAt = u.DoubleUploadExpiresAt,
                IsNoHRActive = u.IsNoHRActive,
                NoHRExpiresAt = u.NoHRExpiresAt,
                TotalSeedingTimeMinutes = u.TotalSeedingTimeMinutes,
                TotalLeechingTimeMinutes = u.TotalLeechingTimeMinutes,
                InvitedBy = u.Invite == null ? null : u.Invite.GeneratorUser!.UserName,
                BanStatus = u.BanStatus,
                BanReason = u.BanReason,
                BanUntil = u.BanUntil,
                SeedingSize = 0, // TODO: Calculate this
                CurrentSeedingCount = 0, // TODO: Calculate this
                CurrentLeechingCount = 0 // TODO: Calculate this
            })
            .ToListAsync();

        return new PaginatedResult<UserAdminProfileDto>
        {
            Items = users,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<bool> MarkCheatLogAsProcessedAsync(int logId, int adminUserId, string? notes = null)
    {
        var log = await _context.CheatLogs.FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            _logger.LogWarning("CheatLog {LogId} not found", logId);
            return false;
        }

        log.IsProcessed = true;
        log.ProcessedAt = DateTimeOffset.UtcNow;
        log.ProcessedByUserId = adminUserId;
        log.AdminNotes = notes?.Substring(0, Math.Min(notes.Length, 500));

        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} marked CheatLog {LogId} as processed", adminUserId, logId);

        return true;
    }

    public async Task<int> MarkCheatLogsBatchAsProcessedAsync(int[] logIds, int adminUserId, string? notes = null)
    {
        var logs = await _context.CheatLogs
            .Where(l => logIds.Contains(l.Id) && !l.IsProcessed)
            .ToListAsync();

        if (!logs.Any())
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var truncatedNotes = notes?.Substring(0, Math.Min(notes.Length, 500));

        foreach (var log in logs)
        {
            log.IsProcessed = true;
            log.ProcessedAt = now;
            log.ProcessedByUserId = adminUserId;
            log.AdminNotes = truncatedNotes;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} marked {Count} CheatLogs as processed", adminUserId, logs.Count);

        return logs.Count;
    }

    public async Task<bool> UnmarkCheatLogAsync(int logId)
    {
        var log = await _context.CheatLogs.FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            _logger.LogWarning("CheatLog {LogId} not found", logId);
            return false;
        }

        log.IsProcessed = false;
        log.ProcessedAt = null;
        log.ProcessedByUserId = null;
        log.AdminNotes = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("CheatLog {LogId} unmarked", logId);

        return true;
    }
}

