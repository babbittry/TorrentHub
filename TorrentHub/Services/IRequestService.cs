using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public interface IRequestService
{
    Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId);
    Task<(bool Success, string Message)> AddBountyAsync(int requestId, AddBountyRequestDto request, int userId);
    Task<List<Request>> GetRequestsAsync(RequestStatus? status);
    Task<(bool Success, string Message)> FillRequestAsync(int requestId, FillRequestDto request, int fillerUserId);
    Task<Request?> GetRequestByIdAsync(int requestId);
}
