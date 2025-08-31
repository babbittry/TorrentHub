
using TorrentHub.Core.DTOs;

namespace TorrentHub.Services.Interfaces;

public interface IPollService
{
    Task<List<PollDto>> GetAllAsync(int? userId);
    Task<PollDto?> GetLatestActiveAsync(int? userId);
    Task<PollDto> CreateAsync(CreatePollDto dto, int createdByUserId);
    Task<PollDto?> GetByIdAsync(int pollId, int? userId);
    Task<(bool Success, string Message)> VoteAsync(int pollId, VoteDto dto, int userId);
    Task<(bool Success, string Message)> DeleteAsync(int pollId);
}

