using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class TorrentCommentService : ITorrentCommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TorrentCommentService> _logger;

    public TorrentCommentService(ApplicationDbContext context, IUserService userService, ISettingsService settingsService, ILogger<TorrentCommentService> logger)
    {
        _context = context;
        _userService = userService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, TorrentComment? Comment)> PostCommentAsync(int torrentId, CreateTorrentCommentRequestDto request, int userId)
    {
        var today = DateTime.UtcNow.Date;

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound", null);
        }

        // Validate parent comment if provided
        TorrentComment? parentTorrentComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentTorrentComment = await _context.TorrentComments
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && c.TorrentId == torrentId);
            
            if (parentTorrentComment == null)
            {
                return (false, "error.comment.parent.notFound", null);
            }
            
            // Check depth limit (max 10 levels)
            if (parentTorrentComment.Depth >= 10)
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
                var maxFloor = await _context.TorrentComments
                    .Where(c => c.TorrentId == torrentId)
                    .MaxAsync(c => (int?)c.Floor) ?? 0;

                var newTorrentComment = new TorrentComment
                {
                    TorrentId = torrentId,
                    UserId = userId,
                    Text = request.Text,
                    Floor = maxFloor + 1,
                    ParentCommentId = request.ParentCommentId,
                    ReplyToUserId = request.ReplyToUserId,
                    Depth = parentTorrentComment?.Depth + 1 ?? 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.TorrentComments.Add(newTorrentComment);

                // Update parent comment reply count
                if (parentTorrentComment != null)
                {
                    parentTorrentComment.ReplyCount++;
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
                    _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId} and earned {Bonus} Coins. Daily count: {Count}", userId, torrentId, settings.CommentBonus, dailyStats.CommentBonusesGiven);
                }
                else
                {
                    _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId}. Daily bonus limit reached.", userId, torrentId);
                }

                await _context.SaveChangesAsync();

                return (true, "comment.post.success", newTorrentComment);
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
        return ex.InnerException?.Message.Contains("IX_TorrentComments_TorrentId_Floor") ?? false;
    }

    public async Task<TorrentCommentListResponse> GetCommentsLazyAsync(int torrentId, int afterFloor = 0, int limit = 30)
    {
        // Limit to maximum 100 items per request
        limit = Math.Min(limit, 100);

        var comments = await _context.TorrentComments
            .Where(c => c.TorrentId == torrentId && c.Floor > afterFloor)
            .Include(c => c.User)
            .Include(c => c.ReplyToUser)
            .OrderBy(c => c.Floor)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        var totalCount = await _context.TorrentComments
            .CountAsync(c => c.TorrentId == torrentId);

        return new TorrentCommentListResponse
        {
            TorrentComments = comments.Select(c => Mappers.Mapper.ToTorrentCommentDto(c)).ToList(),
            HasMore = afterFloor + limit < totalCount,
            TotalCount = totalCount,
            LoadedCount = afterFloor + comments.Count
        };
    }


    public async Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateTorrentCommentRequestDto request, int userId)
    {
        var comment = await _context.TorrentComments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "error.comment.notFound");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || (comment.UserId != userId && user.Role != UserRole.Administrator))
        {
            return (false, "error.unauthorized");
        }

        comment.Text = request.Content;
        comment.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("TorrentComment {TorrentCommentId} updated by user {UserId}.", commentId, userId);
        return (true, "comment.update.success");
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.TorrentComments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "error.comment.notFound");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || (comment.UserId != userId && user.Role != UserRole.Administrator))
        {
            return (false, "error.unauthorized");
        }

        // Update parent comment reply count if exists
        if (comment.ParentCommentId.HasValue)
        {
            var parent = await _context.TorrentComments.FindAsync(comment.ParentCommentId.Value);
            if (parent != null)
            {
                parent.ReplyCount = await _context.TorrentComments
                    .CountAsync(c => c.ParentCommentId == comment.ParentCommentId.Value && c.Id != commentId);
            }
        }

        _context.TorrentComments.Remove(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("TorrentComment {TorrentCommentId} deleted by user {UserId}.", commentId, userId);
        return (true, "comment.delete.success");
    }
}
