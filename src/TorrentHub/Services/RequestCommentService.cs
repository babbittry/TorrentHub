using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class RequestCommentService : IRequestCommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ISettingsService _settingsService;
    private readonly IReactionService _reactionService;
    private readonly ILogger<RequestCommentService> _logger;

    public RequestCommentService(
        ApplicationDbContext context,
        IUserService userService,
        ISettingsService settingsService,
        IReactionService reactionService,
        ILogger<RequestCommentService> logger)
    {
        _context = context;
        _userService = userService;
        _settingsService = settingsService;
        _reactionService = reactionService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, RequestComment? Comment)> PostCommentAsync(int requestId, CreateRequestCommentRequestDto request, int userId)
    {
        var today = DateTime.UtcNow.Date;

        var requestEntity = await _context.Requests.FindAsync(requestId);
        if (requestEntity == null)
        {
            return (false, "error.request.notFound", null);
        }

        // Validate parent comment if provided
        RequestComment? parentRequestComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentRequestComment = await _context.RequestComments
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && c.RequestId == requestId);
            
            if (parentRequestComment == null)
            {
                return (false, "error.comment.parent.notFound", null);
            }
            
            // Check depth limit (max 10 levels)
            if (parentRequestComment.Depth >= 10)
            {
                return (false, "error.comment.maxDepth", null);
            }
        }

        // Get next Floor number with retry for concurrency
        const int maxRetries = 3;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var maxFloor = await _context.RequestComments
                    .Where(c => c.RequestId == requestId)
                    .MaxAsync(c => (int?)c.Floor) ?? 0;

                var newRequestComment = new RequestComment
                {
                    RequestId = requestId,
                    UserId = userId,
                    Text = request.Text,
                    Floor = maxFloor + 1,
                    ParentCommentId = request.ParentCommentId,
                    ReplyToUserId = request.ReplyToUserId,
                    Depth = parentRequestComment?.Depth + 1 ?? 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.RequestComments.Add(newRequestComment);

                // Update parent comment reply count
                if (parentRequestComment != null)
                {
                    parentRequestComment.ReplyCount++;
                }

                var settings = await _settingsService.GetSiteSettingsAsync();

                var dailyStats = await _context.UserDailyStats
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == today);

                if (dailyStats == null)
                {
                    dailyStats = new UserDailyStats { UserId = userId, Date = today };
                    _context.UserDailyStats.Add(dailyStats);
                }

                if (dailyStats.CommentBonusesGiven < settings.MaxDailyCommentBonuses)
                {
                    dailyStats.CommentBonusesGiven++;
                    await _userService.AddCoinsAsync(userId, new UpdateCoinsRequestDto { Amount = settings.CommentBonus });
                    _logger.LogInformation("User {UserId} posted a comment on request {RequestId} and earned {Bonus} Coins. Daily count: {Count}", userId, requestId, settings.CommentBonus, dailyStats.CommentBonusesGiven);
                }
                else
                {
                    _logger.LogInformation("User {UserId} posted a comment on request {RequestId}. Daily bonus limit reached.", userId, requestId);
                }

                await _context.SaveChangesAsync();

                return (true, "comment.post.success", newRequestComment);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && retry < maxRetries - 1)
            {
                // Floor conflict, retry with a short delay
                await Task.Delay(Random.Shared.Next(20, 50));
            }
        }

        return (false, "error.comment.create.failed", null);
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_RequestComments_RequestId_Floor") ?? false;
    }

    public async Task<RequestCommentListResponse> GetCommentsLazyAsync(int requestId, int afterFloor = 0, int limit = 30)
    {
        // Limit to maximum 100 items per request
        limit = Math.Min(limit, 100);

        var comments = await _context.RequestComments
            .Where(c => c.RequestId == requestId && c.Floor > afterFloor)
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .OrderBy(c => c.Floor)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        var totalCount = await _context.RequestComments
            .CountAsync(c => c.RequestId == requestId);

        var commentDtos = comments.Select(c => Mappers.Mapper.ToRequestCommentDto(c)).ToList();

        // Batch load reactions for all comments
        if (commentDtos.Any())
        {
            var commentIds = commentDtos.Select(c => c.Id).ToList();
            var reactionsDict = await _reactionService.GetReactionsBatchAsync("RequestComment", commentIds, null);
            
            foreach (var comment in commentDtos)
            {
                if (reactionsDict.TryGetValue(comment.Id, out var reactions))
                {
                    comment.Reactions = reactions;
                }
            }
        }

        return new RequestCommentListResponse
        {
            Items = commentDtos,
            HasMore = afterFloor + limit < totalCount,
            TotalCount = totalCount,
            LoadedCount = afterFloor + comments.Count
        };
    }


    public async Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateRequestCommentRequestDto request, int userId)
    {
        var comment = await _context.RequestComments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "Comment not found");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        
        // 验证编辑权限
        var (canEdit, errorMessage) = ContentModerationHelper.CanEditContent(
            authorId: comment.UserId,
            currentUserId: userId,
            userRole: user.Role,
            replyCount: comment.ReplyCount,
            createdAt: comment.CreatedAt,
            editWindowMinutes: settings.ContentEditWindowMinutes
        );

        if (!canEdit)
        {
            return (false, errorMessage);
        }

        comment.Text = request.Content;
        comment.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "RequestComment {CommentId} updated by user {UserId} (Role: {Role}).",
            commentId, userId, user.Role);
        return (true, "Comment updated successfully");
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.RequestComments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "Comment not found");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        
        // 验证删除权限
        var (canDelete, errorMessage) = ContentModerationHelper.CanDeleteContent(
            authorId: comment.UserId,
            currentUserId: userId,
            userRole: user.Role,
            replyCount: comment.ReplyCount,
            createdAt: comment.CreatedAt,
            editWindowMinutes: settings.ContentEditWindowMinutes
        );

        if (!canDelete)
        {
            return (false, errorMessage);
        }

        // Update parent comment reply count if exists
        if (comment.ParentCommentId.HasValue)
        {
            var parent = await _context.RequestComments.FindAsync(comment.ParentCommentId.Value);
            if (parent != null)
            {
                parent.ReplyCount = Math.Max(0, parent.ReplyCount - 1);
            }
        }

        _context.RequestComments.Remove(comment);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "RequestComment {CommentId} deleted by user {UserId} (Role: {Role}).",
            commentId, userId, user.Role);
        return (true, "Comment deleted successfully");
    }
}