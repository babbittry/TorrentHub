using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services.Interfaces;

public interface ICommentService
{
    Task<(bool Success, string Message, Comment? Comment)> PostCommentAsync(int torrentId, CreateCommentRequestDto request, int userId);
    Task<IEnumerable<Comment>> GetCommentsForTorrentAsync(int torrentId, int page, int pageSize);
    Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateCommentRequestDto request, int userId);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId);
}
