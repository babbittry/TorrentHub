using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services.Interfaces;

public interface IRequestCommentService
{
    Task<(bool Success, string Message, RequestComment? Comment)> PostCommentAsync(int requestId, CreateRequestCommentRequestDto request, int userId);
    Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateRequestCommentRequestDto request, int userId);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId);
    Task<RequestCommentListResponse> GetCommentsLazyAsync(int requestId, int afterFloor = 0, int limit = 30);
}