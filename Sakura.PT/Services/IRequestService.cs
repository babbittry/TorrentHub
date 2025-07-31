using Sakura.PT.DTOs;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public interface IRequestService
{
    Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId);
    Task<(bool Success, string Message)> AddBountyAsync(int requestId, long amount, int userId);
    Task<List<Request>> GetRequestsAsync(RequestStatus? status);
    Task<(bool Success, string Message)> FillRequestAsync(int requestId, int torrentId, int fillerUserId);
}
