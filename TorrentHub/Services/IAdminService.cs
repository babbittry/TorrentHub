
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IAdminService
{
    // Client Banning
    Task<List<BannedClient>> GetBannedClientsAsync();
    Task<BannedClient> AddBannedClientAsync(BannedClientDto dto);
    Task<(bool Success, string Message)> DeleteBannedClientAsync(int id);

    // Duplicate IP Detection
    Task<List<DuplicateIpUserDto>> GetDuplicateIpUsersAsync();

    // Cheating Detection
    Task LogCheatAsync(int userId, string reason, string details);
    Task<List<CheatLogDto>> GetCheatLogsAsync();

    // Log Viewing
    Task<List<System.Text.Json.JsonDocument>> SearchSystemLogsAsync(LogSearchDto dto);

    // User Management
    Task<PaginatedResult<UserProfileDetailDto>> GetUsersAsync(int page, int pageSize);
}
