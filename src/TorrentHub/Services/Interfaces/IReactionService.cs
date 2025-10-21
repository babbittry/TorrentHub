using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services.Interfaces;

public interface IReactionService
{
    Task<(bool Success, string Message)> AddReactionAsync(
        string commentType, 
        int commentId, 
        ReactionType type, 
        int userId);
    
    Task<(bool Success, string Message)> RemoveReactionAsync(
        string commentType, 
        int commentId, 
        ReactionType type, 
        int userId);
    
    Task<CommentReactionsDto> GetReactionsAsync(
        string commentType, 
        int commentId, 
        int? viewerUserId = null);
    
    Task<Dictionary<int, CommentReactionsDto>> GetReactionsBatchAsync(
        string commentType, 
        List<int> commentIds, 
        int? viewerUserId = null);
}