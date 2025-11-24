using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ISettingsService _settingsService;
    private readonly IReactionService _reactionService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ApplicationDbContext context,
        IUserService userService,
        ISettingsService settingsService,
        IReactionService reactionService,
        ILogger<CommentService> logger)
    {
        _context = context;
        _userService = userService;
        _settingsService = settingsService;
        _reactionService = reactionService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, CommentDto? Comment)> PostCommentAsync(
        CommentableType commentableType,
        int commentableId,
        CreateCommentDto request,
        int userId)
    {
        var today = DateTime.UtcNow.Date;

        // Validate commentable exists
        if (!await ValidateCommentableExistsAsync(commentableType, commentableId))
        {
            return (false, $"error.{commentableType.ToString().ToLower()}.notFound", null);
        }

        // Validate parent comment if provided
        Comment? parentComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentComment = await _context.Comments
                .FirstOrDefaultAsync(c => 
                    c.Id == request.ParentCommentId.Value && 
                    c.CommentableType == commentableType &&
                    c.CommentableId == commentableId);
            
            if (parentComment == null)
            {
                return (false, "error.comment.parent.notFound", null);
            }
            
            // Check depth limit (max 10 levels)
            if (parentComment.Depth >= 10)
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
                var maxFloor = await _context.Comments
                    .Where(c => c.CommentableType == commentableType && c.CommentableId == commentableId)
                    .MaxAsync(c => (int?)c.Floor) ?? 0;

                var newComment = new Comment
                {
                    CommentableType = commentableType,
                    CommentableId = commentableId,
                    UserId = userId,
                    Content = request.Content,
                    Floor = maxFloor + 1,
                    ParentCommentId = request.ParentCommentId,
                    ReplyToUserId = request.ReplyToUserId,
                    Depth = parentComment?.Depth + 1 ?? 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.Comments.Add(newComment);

                // Update parent comment reply count
                if (parentComment != null)
                {
                    parentComment.ReplyCount++;
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
                    _logger.LogInformation(
                        "User {UserId} posted a comment on {Type}:{Id} and earned {Bonus} Coins. Daily count: {Count}",
                        userId, commentableType, commentableId, settings.CommentBonus, dailyStats.CommentBonusesGiven);
                }
                else
                {
                    _logger.LogInformation(
                        "User {UserId} posted a comment on {Type}:{Id}. Daily bonus limit reached.",
                        userId, commentableType, commentableId);
                }

                // 如果是论坛评论,更新主题的最后发帖时间
                if (commentableType == CommentableType.ForumTopic)
                {
                    var topic = await _context.ForumTopics.FindAsync(commentableId);
                    if (topic != null)
                    {
                        topic.LastPostTime = DateTimeOffset.UtcNow.UtcDateTime;
                        _logger.LogInformation(
                            "Updated ForumTopic {TopicId} LastPostTime after comment posted by user {UserId}",
                            commentableId, userId);
                    }
                }

                await _context.SaveChangesAsync();

                // Load navigation properties and convert to DTO
                await _context.Entry(newComment).Reference(c => c.User).LoadAsync();
                if (newComment.ReplyToUserId.HasValue)
                {
                    await _context.Entry(newComment).Reference(c => c.ReplyToUser).LoadAsync();
                }
                
                var commentDto = Mappers.Mapper.ToCommentDto(newComment);
                return (true, "comment.post.success", commentDto);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && retry < maxRetries - 1)
            {
                // Floor conflict, retry with a short delay
                await Task.Delay(Random.Shared.Next(20, 50));
            }
        }

        return (false, "error.comment.create.failed", null);
    }

    public async Task<(bool Success, string Message, CommentDto? Comment)> UpdateCommentAsync(
        int commentId,
        UpdateCommentDto request,
        int userId)
    {
        var comment = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .FirstOrDefaultAsync(c => c.Id == commentId);
            
        if (comment == null)
        {
            return (false, "Comment not found", null);
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        
        // Validate edit permissions
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
            return (false, errorMessage, null);
        }

        comment.Content = request.Content;
        comment.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Comment {CommentId} ({Type}:{Id}) updated by user {UserId} (Role: {Role}).",
            commentId, comment.CommentableType, comment.CommentableId, userId, user.Role);
        
        var commentDto = Mappers.Mapper.ToCommentDto(comment);
        return (true, "Comment updated successfully", commentDto);
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
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
        
        // Validate delete permissions
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
            var parent = await _context.Comments.FindAsync(comment.ParentCommentId.Value);
            if (parent != null)
            {
                parent.ReplyCount = Math.Max(0, parent.ReplyCount - 1);
            }
        }

        _context.Comments.Remove(comment);

        // 如果是论坛评论,更新主题的最后发帖时间
        if (comment.CommentableType == CommentableType.ForumTopic)
        {
            var topic = await _context.ForumTopics.FindAsync(comment.CommentableId);
            if (topic != null)
            {
                // 获取该主题的最后一条评论时间
                var lastComment = await _context.Comments
                    .Where(c => c.CommentableType == CommentableType.ForumTopic &&
                               c.CommentableId == comment.CommentableId &&
                               c.Id != commentId)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                
                topic.LastPostTime = lastComment?.CreatedAt.UtcDateTime ?? topic.CreatedAt.UtcDateTime;
                
                _logger.LogInformation(
                    "Updated ForumTopic {TopicId} LastPostTime after comment {CommentId} deleted",
                    comment.CommentableId, commentId);
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Comment {CommentId} ({Type}:{Id}) deleted by user {UserId} (Role: {Role}).",
            commentId, comment.CommentableType, comment.CommentableId, userId, user.Role);
        
        return (true, "Comment deleted successfully");
    }

    public async Task<CommentListResponse> GetCommentsLazyAsync(
        CommentableType commentableType, 
        int commentableId, 
        int afterFloor = 0, 
        int limit = 30)
    {
        // Limit to maximum 100 items per request
        limit = Math.Min(limit, 100);

        var comments = await _context.Comments
            .Where(c => 
                c.CommentableType == commentableType && 
                c.CommentableId == commentableId && 
                c.Floor > afterFloor)
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .OrderBy(c => c.Floor)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        var totalCount = await _context.Comments
            .CountAsync(c => c.CommentableType == commentableType && c.CommentableId == commentableId);

        var commentDtos = comments.Select(c => Mappers.Mapper.ToCommentDto(c)).ToList();

        // Batch load reactions for all comments
        if (commentDtos.Any())
        {
            var commentIds = commentDtos.Select(c => c.Id).ToList();
            var reactionsDict = await _reactionService.GetReactionsBatchAsync(commentIds, null);
            
            foreach (var comment in commentDtos)
            {
                if (reactionsDict.TryGetValue(comment.Id, out var reactions))
                {
                    comment.Reactions = reactions;
                }
            }
        }

        return new CommentListResponse
        {
            Items = commentDtos,
            HasMore = afterFloor + limit < totalCount,
            TotalItems = totalCount,
            LoadedCount = afterFloor + comments.Count
        };
    }

    public async Task<CommentDto?> GetCommentByIdAsync(int commentId)
    {
        var comment = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return null;
        }

        var commentDto = Mappers.Mapper.ToCommentDto(comment);

        // Load reactions
        var reactions = await _reactionService.GetReactionsAsync(commentId, null);
        commentDto.Reactions = reactions;

        return commentDto;
    }

    public async Task<CommentListResponse> GetUserCommentsAsync(int userId, int page = 1, int pageSize = 30)
    {
        pageSize = Math.Min(pageSize, 100);
        var skip = (page - 1) * pageSize;

        var comments = await _context.Comments
            .Where(c => c.UserId == userId)
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        var totalCount = await _context.Comments.CountAsync(c => c.UserId == userId);

        var commentDtos = comments.Select(c => Mappers.Mapper.ToCommentDto(c)).ToList();

        return new CommentListResponse
        {
            Items = commentDtos,
            HasMore = skip + pageSize < totalCount,
            TotalItems = totalCount,
            LoadedCount = skip + comments.Count
        };
    }

    private async Task<bool> ValidateCommentableExistsAsync(CommentableType type, int id)
    {
        return type switch
        {
            CommentableType.Torrent => await _context.Torrents.AnyAsync(t => t.Id == id),
            CommentableType.Request => await _context.Requests.AnyAsync(r => r.Id == id),
            CommentableType.ForumTopic => await _context.ForumTopics.AnyAsync(t => t.Id == id),
            _ => false
        };
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_Comments_Unique_Floor") ?? false;
    }
}