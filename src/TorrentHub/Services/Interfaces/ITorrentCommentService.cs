using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services.Interfaces;

public interface ITorrentCommentService
{
    Task<(bool Success, string Message, TorrentComment? Comment)> PostCommentAsync(int torrentId, CreateTorrentCommentRequestDto request, int userId);
    Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateTorrentCommentRequestDto request, int userId);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId);
    Task<TorrentCommentListResponse> GetCommentsLazyAsync(int torrentId, int afterFloor = 0, int limit = 30);
}