using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services.Interfaces;

public interface IReactionService
{
    Task<(bool Success, string Message)> AddReactionAsync(
        int commentId,
        ReactionType type,
        int userId);
    
    Task<(bool Success, string Message)> RemoveReactionAsync(
        int commentId,
        ReactionType type,
        int userId);
    
    Task<CommentReactionsDto> GetReactionsAsync(
        int commentId,
        int? viewerUserId = null);
    
    Task<Dictionary<int, CommentReactionsDto>> GetReactionsBatchAsync(
        List<int> commentIds,
        int? viewerUserId = null);
}