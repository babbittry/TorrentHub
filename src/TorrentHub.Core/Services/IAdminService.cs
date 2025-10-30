
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Entities;

namespace TorrentHub.Core.Services;

public interface IAdminService
{
    // Client Banning
    Task<List<BannedClient>> GetBannedClientsAsync();
    Task<BannedClient> AddBannedClientAsync(BannedClientDto dto);
    Task<(bool Success, string Message)> DeleteBannedClientAsync(int id);

    // Duplicate IP Detection
    Task<List<DuplicateIpUserDto>> GetDuplicateIpUsersAsync();

    // Cheating Detection
    Task LogCheatAsync(
        int userId,
        CheatDetectionType detectionType,
        CheatSeverity severity,
        string? details = null,
        int? torrentId = null,
        string? ipAddress = null);
    
    Task<PaginatedResult<CheatLogDto>> GetCheatLogsAsync(
        int page = 1,
        int pageSize = 50,
        int? userId = null,
        string? detectionType = null);
    
    // CheatLog 处理状态管理
    /// <summary>
    /// 标记作弊日志为已处理
    /// </summary>
    Task<bool> MarkCheatLogAsProcessedAsync(int logId, int adminUserId, string? notes = null);
    
    /// <summary>
    /// 批量标记作弊日志为已处理
    /// </summary>
    Task<int> MarkCheatLogsBatchAsProcessedAsync(int[] logIds, int adminUserId, string? notes = null);
    
    /// <summary>
    /// 取消作弊日志的处理状态
    /// </summary>
    Task<bool> UnmarkCheatLogAsync(int logId);

    // Log Viewing
    Task<List<System.Text.Json.JsonDocument>> SearchSystemLogsAsync(LogSearchDto dto);

    // User Management
    Task<PaginatedResult<UserAdminProfileDto>> GetUsersAsync(int page, int pageSize);
}

