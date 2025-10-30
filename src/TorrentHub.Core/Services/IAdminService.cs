
using TorrentHub.Core.DTOs;
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
        string detectionType,
        string? details = null,
        int? torrentId = null,
        string? ipAddress = null);
    
    Task<PaginatedResult<CheatLogDto>> GetCheatLogsAsync(
        int page = 1,
        int pageSize = 50,
        int? userId = null,
        string? detectionType = null);

    // Log Viewing
    Task<List<System.Text.Json.JsonDocument>> SearchSystemLogsAsync(LogSearchDto dto);

    // User Management
    Task<PaginatedResult<UserAdminProfileDto>> GetUsersAsync(int page, int pageSize);
}

