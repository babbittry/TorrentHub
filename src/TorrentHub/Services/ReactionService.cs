using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class ReactionService : IReactionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReactionService> _logger;

    public ReactionService(ApplicationDbContext context, ILogger<ReactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> AddReactionAsync(
        string commentType, 
        int commentId, 
        ReactionType type, 
        int userId)
    {
        // Validate comment type
        if (commentType != "TorrentComment" && commentType != "ForumPost")
        {
            return (false, "error.reaction.invalidCommentType");
        }

        // Verify comment exists
        var commentExists = commentType == "TorrentComment"
            ? await _context.TorrentComments.AnyAsync(c => c.Id == commentId)
            : await _context.ForumPosts.AnyAsync(p => p.Id == commentId);

        if (!commentExists)
        {
            return (false, "error.reaction.commentNotFound");
        }

        // Check if reaction already exists (idempotent)
        var existingReaction = await _context.CommentReactions
            .FirstOrDefaultAsync(r => 
                r.CommentType == commentType && 
                r.CommentId == commentId && 
                r.UserId == userId && 
                r.Type == type);

        if (existingReaction != null)
        {
            // Already reacted, return success (idempotent)
            return (true, "reaction.added");
        }

        // Add new reaction
        var reaction = new CommentReaction
        {
            CommentType = commentType,
            CommentId = commentId,
            UserId = userId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.CommentReactions.Add(reaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added reaction {Type} to {CommentType} {CommentId}", 
            userId, type, commentType, commentId);

        return (true, "reaction.added");
    }

    public async Task<(bool Success, string Message)> RemoveReactionAsync(
        string commentType, 
        int commentId, 
        ReactionType type, 
        int userId)
    {
        // Validate comment type
        if (commentType != "TorrentComment" && commentType != "ForumPost")
        {
            return (false, "error.reaction.invalidCommentType");
        }

        var reaction = await _context.CommentReactions
            .FirstOrDefaultAsync(r => 
                r.CommentType == commentType && 
                r.CommentId == commentId && 
                r.UserId == userId && 
                r.Type == type);

        if (reaction == null)
        {
            // Not found, return success (idempotent)
            return (true, "reaction.removed");
        }

        _context.CommentReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} removed reaction {Type} from {CommentType} {CommentId}", 
            userId, type, commentType, commentId);

        return (true, "reaction.removed");
    }

    public async Task<CommentReactionsDto> GetReactionsAsync(
        string commentType, 
        int commentId, 
        int? viewerUserId = null)
    {
        var reactions = await _context.CommentReactions
            .Where(r => r.CommentType == commentType && r.CommentId == commentId)
            .Include(r => r.User)
            .AsNoTracking()
            .ToListAsync();

        var groupedReactions = reactions
            .GroupBy(r => r.Type)
            .Select(g => new ReactionSummaryDto
            {
                Type = g.Key,
                Count = g.Count(),
                ViewerReacted = viewerUserId.HasValue && g.Any(r => r.UserId == viewerUserId.Value),
                Users = g.Take(10).Select(r => new UserDisplayDto
                {
                    Id = r.User!.Id,
                    Username = r.User.UserName,
                    Avatar = r.User.Avatar
                }).ToList()
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        return new CommentReactionsDto
        {
            TotalCount = reactions.Count,
            Reactions = groupedReactions
        };
    }

    public async Task<Dictionary<int, CommentReactionsDto>> GetReactionsBatchAsync(
        string commentType, 
        List<int> commentIds, 
        int? viewerUserId = null)
    {
        if (commentIds == null || commentIds.Count == 0)
        {
            return new Dictionary<int, CommentReactionsDto>();
        }

        // Limit batch size
        commentIds = commentIds.Take(100).ToList();

        var reactions = await _context.CommentReactions
            .Where(r => r.CommentType == commentType && commentIds.Contains(r.CommentId))
            .Include(r => r.User)
            .AsNoTracking()
            .ToListAsync();

        var result = new Dictionary<int, CommentReactionsDto>();

        foreach (var commentId in commentIds)
        {
            var commentReactions = reactions.Where(r => r.CommentId == commentId).ToList();
            
            var groupedReactions = commentReactions
                .GroupBy(r => r.Type)
                .Select(g => new ReactionSummaryDto
                {
                    Type = g.Key,
                    Count = g.Count(),
                    ViewerReacted = viewerUserId.HasValue && g.Any(r => r.UserId == viewerUserId.Value),
                    Users = new List<UserDisplayDto>() // Don't include users in batch request for performance
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            result[commentId] = new CommentReactionsDto
            {
                TotalCount = commentReactions.Count,
                Reactions = groupedReactions
            };
        }

        return result;
    }
}