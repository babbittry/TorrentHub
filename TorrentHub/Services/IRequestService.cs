using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public interface IRequestService
{
    Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId);
    Task<(bool Success, string Message)> AddBountyAsync(int requestId, AddBountyRequestDto request, int userId);
    Task<List<Request>> GetRequestsAsync(RequestStatus? status, string sortBy, string sortOrder);
    Task<(bool Success, string Message)> FillRequestAsync(int requestId, FillRequestDto request, int fillerUserId);
    Task<Request?> GetRequestByIdAsync(int requestId);

    Task<(bool Success, string Message)> ConfirmFulfillmentAsync(int requestId, int userId);
    Task<(bool Success, string Message)> RejectFulfillmentAsync(int requestId, RejectFulfillmentDto rejectDto, int userId);
    Task AutoCompleteExpiredConfirmationsAsync();
}
