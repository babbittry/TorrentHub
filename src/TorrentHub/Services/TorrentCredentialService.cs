using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;

namespace TorrentHub.Services;

public class TorrentCredentialService : ITorrentCredentialService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TorrentCredentialService> _logger;

    public TorrentCredentialService(
        ApplicationDbContext context,
        ILogger<TorrentCredentialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> GetOrCreateCredentialAsync(int userId, int torrentId)
    {
        // 首先尝试获取现有的未撤销credential
        var existingCredential = await _context.TorrentCredentials
            .Where(tc => tc.UserId == userId && tc.TorrentId == torrentId && !tc.IsRevoked)
            .FirstOrDefaultAsync();

        if (existingCredential != null)
        {
            _logger.LogDebug("Reusing existing credential for User {UserId} and Torrent {TorrentId}", userId, torrentId);
            return existingCredential.Credential;
        }

        // 加载User和Torrent实体以满足required导航属性
        var user = await _context.Users.FindAsync(userId);
        var torrent = await _context.Torrents.FindAsync(torrentId);

        if (user == null)
        {
            throw new ArgumentException($"User with ID {userId} not found", nameof(userId));
        }

        if (torrent == null)
        {
            throw new ArgumentException($"Torrent with ID {torrentId} not found", nameof(torrentId));
        }

        // 创建新的credential
        var newCredential = new TorrentCredential
        {
            Credential = Guid.NewGuid(),
            UserId = userId,
            User = user,
            TorrentId = torrentId,
            Torrent = torrent,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false,
            UsageCount = 0
        };

        _context.TorrentCredentials.Add(newCredential);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new credential {Credential} for User {UserId} and Torrent {TorrentId}",
            newCredential.Credential, userId, torrentId);

        return newCredential.Credential;
    }

    public async Task<(bool IsValid, int? UserId, int? TorrentId)> ValidateCredentialAsync(Guid credential)
    {
        var torrentCredential = await _context.TorrentCredentials
            .Where(tc => tc.Credential == credential && !tc.IsRevoked)
            .FirstOrDefaultAsync();

        if (torrentCredential == null)
        {
            _logger.LogWarning("Invalid or revoked credential attempted: {Credential}", credential);
            return (false, null, null);
        }

        return (true, torrentCredential.UserId, torrentCredential.TorrentId);
    }

    public async Task<bool> RevokeCredentialAsync(Guid credential, string reason)
    {
        var torrentCredential = await _context.TorrentCredentials
            .Where(tc => tc.Credential == credential)
            .FirstOrDefaultAsync();

        if (torrentCredential == null)
        {
            _logger.LogWarning("Attempted to revoke non-existent credential: {Credential}", credential);
            return false;
        }

        if (torrentCredential.IsRevoked)
        {
            _logger.LogInformation("Credential {Credential} is already revoked", credential);
            return true;
        }

        torrentCredential.IsRevoked = true;
        torrentCredential.RevokedAt = DateTimeOffset.UtcNow;
        torrentCredential.RevokeReason = reason?.Substring(0, Math.Min(reason.Length, 200));

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked credential {Credential} for User {UserId} and Torrent {TorrentId}. Reason: {Reason}",
            credential, torrentCredential.UserId, torrentCredential.TorrentId, reason);

        return true;
    }

    public async Task<int> RevokeUserTorrentCredentialsAsync(int userId, int torrentId, string reason)
    {
        var credentials = await _context.TorrentCredentials
            .Where(tc => tc.UserId == userId && tc.TorrentId == torrentId && !tc.IsRevoked)
            .ToListAsync();

        if (!credentials.Any())
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var truncatedReason = reason?.Substring(0, Math.Min(reason.Length, 200));

        foreach (var credential in credentials)
        {
            credential.IsRevoked = true;
            credential.RevokedAt = now;
            credential.RevokeReason = truncatedReason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked {Count} credentials for User {UserId} and Torrent {TorrentId}. Reason: {Reason}",
            credentials.Count, userId, torrentId, reason);

        return credentials.Count;
    }

    public async Task<(int Count, List<int> TorrentIds)> RevokeUserCredentialsAsync(int userId, string reason)
    {
        var credentials = await _context.TorrentCredentials
            .Where(tc => tc.UserId == userId && !tc.IsRevoked)
            .ToListAsync();

        if (!credentials.Any())
        {
            return (0, new List<int>());
        }

        var now = DateTimeOffset.UtcNow;
        var truncatedReason = reason?.Substring(0, Math.Min(reason.Length, 200));
        var torrentIds = credentials.Select(c => c.TorrentId).Distinct().ToList();

        foreach (var credential in credentials)
        {
            credential.IsRevoked = true;
            credential.RevokedAt = now;
            credential.RevokeReason = truncatedReason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked {Count} credentials for User {UserId}. Reason: {Reason}",
            credentials.Count, userId, reason);

        return (credentials.Count, torrentIds);
    }

    public async Task UpdateCredentialUsageAsync(Guid credential)
    {
        var torrentCredential = await _context.TorrentCredentials
            .Where(tc => tc.Credential == credential && !tc.IsRevoked)
            .FirstOrDefaultAsync();

        if (torrentCredential == null)
        {
            _logger.LogWarning("Attempted to update usage for invalid credential: {Credential}", credential);
            return;
        }

        torrentCredential.LastUsedAt = DateTimeOffset.UtcNow;
        torrentCredential.UsageCount++;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated usage for credential {Credential}. New count: {UsageCount}", 
            credential, torrentCredential.UsageCount);
    }

    public async Task<(List<TorrentCredential> Items, int TotalCount)> GetUserCredentialsAsync(
        int userId,
        CredentialFilterRequest filter)
    {
        var query = _context.TorrentCredentials
            .Include(tc => tc.Torrent)
            .Where(tc => tc.UserId == userId);

        // 应用撤销状态筛选
        if (filter.OnlyRevoked)
        {
            query = query.Where(tc => tc.IsRevoked);
        }
        else if (!filter.IncludeRevoked)
        {
            query = query.Where(tc => !tc.IsRevoked);
        }

        // 应用关键字搜索
        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var keyword = filter.SearchKeyword.ToLower();
            query = query.Where(tc => tc.Torrent!.Name.ToLower().Contains(keyword));
        }

        // 获取总数 (在应用筛选后)
        var totalCount = await query.CountAsync();

        // 应用排序
        query = filter.SortBy switch
        {
            CredentialSortBy.CreatedAt => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.CreatedAt)
                : query.OrderByDescending(tc => tc.CreatedAt),

            CredentialSortBy.LastUsedAt => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.LastUsedAt ?? DateTimeOffset.MinValue)
                : query.OrderByDescending(tc => tc.LastUsedAt ?? DateTimeOffset.MinValue),

            CredentialSortBy.UsageCount => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.UsageCount)
                : query.OrderByDescending(tc => tc.UsageCount),

            CredentialSortBy.TorrentName => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.Torrent!.Name)
                : query.OrderByDescending(tc => tc.Torrent!.Name),

            CredentialSortBy.TotalUploadedBytes => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.TotalUploadedBytes)
                : query.OrderByDescending(tc => tc.TotalUploadedBytes),

            CredentialSortBy.TotalDownloadedBytes => filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(tc => tc.TotalDownloadedBytes)
                : query.OrderByDescending(tc => tc.TotalDownloadedBytes),

            _ => query.OrderByDescending(tc => tc.CreatedAt)
        };

        // 应用分页
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} credentials for User {UserId} (Total: {TotalCount})",
            items.Count, userId, totalCount);

        return (items, totalCount);
    }

    public async Task<int> CleanupInactiveCredentialsAsync(int inactiveDays = 90)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-inactiveDays);

        // 删除满足以下条件的credentials:
        // 1. 未撤销
        // 2. 从未使用过或最后使用时间超过inactiveDays天
        var inactiveCredentials = await _context.TorrentCredentials
            .Where(tc => !tc.IsRevoked && 
                   (tc.LastUsedAt == null && tc.CreatedAt < cutoffDate ||
                    tc.LastUsedAt != null && tc.LastUsedAt < cutoffDate))
            .ToListAsync();

        if (!inactiveCredentials.Any())
        {
            _logger.LogInformation("No inactive credentials to clean up");
            return 0;
        }

        _context.TorrentCredentials.RemoveRange(inactiveCredentials);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} inactive credentials (inactive for {Days} days)", 
            inactiveCredentials.Count, inactiveDays);

        return inactiveCredentials.Count;
    }
    public async Task<(int Count, List<int> TorrentIds)> RevokeBatchAsync(int userId, Guid[] credentialIds, string reason)
    {
        var credentials = await _context.TorrentCredentials
            .Where(c => c.UserId == userId && credentialIds.Contains(c.Credential) && !c.IsRevoked)
            .ToListAsync();

        var foundIds = credentials.Select(c => c.Credential).ToList();
        var failedIds = credentialIds.Except(foundIds).ToList();

        if (!credentials.Any())
        {
            return (0, new List<int>());
        }
        var torrentIds = credentials.Select(c => c.TorrentId).Distinct().ToList();

        var now = DateTimeOffset.UtcNow;
        var truncatedReason = reason?.Substring(0, Math.Min(reason.Length, 200));

        foreach (var credential in credentials)
        {
            credential.IsRevoked = true;
            credential.RevokedAt = now;
            credential.RevokeReason = truncatedReason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} revoked a batch of {Count} credentials. Reason: {Reason}",
            userId, credentials.Count, reason);

        return (credentials.Count, torrentIds);
    }
    public async Task<(int Count, List<int> TorrentIds)> AdminRevokeBatchAsync(Guid[] credentialIds, string reason)
    {
        var credentials = await _context.TorrentCredentials
            .Where(c => credentialIds.Contains(c.Credential) && !c.IsRevoked)
            .ToListAsync();

        if (!credentials.Any())
        {
            return (0, new List<int>());
        }

        var torrentIds = credentials.Select(c => c.TorrentId).Distinct().ToList();
        var now = DateTimeOffset.UtcNow;
        var truncatedReason = reason?.Substring(0, Math.Min(reason.Length, 200));

        foreach (var credential in credentials)
        {
            credential.IsRevoked = true;
            credential.RevokedAt = now;
            credential.RevokeReason = truncatedReason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin revoked a batch of {Count} credentials. Reason: {Reason}",
            credentials.Count, reason);

        return (credentials.Count, torrentIds);
    }
}