using Microsoft.EntityFrameworkCore;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(ApplicationDbContext context, IUserService userService, ISettingsService settingsService, ILogger<CommentService> logger)
    {
        _context = context;
        _userService = userService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Comment? Comment)> PostCommentAsync(int torrentId, CreateCommentRequestDto request, int userId)
    {
        var today = DateTime.UtcNow.Date;

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound", null);
        }

        var newComment = new Comment
        {
            TorrentId = torrentId,
            UserId = userId,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Comments.Add(newComment);

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

        return (true, "comment.post.success", newComment);
    }

    public async Task<IEnumerable<Comment>> GetCommentsForTorrentAsync(int torrentId, int page, int pageSize)
    {
        return await _context.Comments
            .Where(c => c.TorrentId == torrentId)
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.User)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateCommentRequestDto request, int userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
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
        _logger.LogInformation("Comment {CommentId} updated by user {UserId}.", commentId, userId);
        return (true, "comment.update.success");
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "error.comment.notFound");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || (comment.UserId != userId && user.Role != UserRole.Administrator))
        {
            return (false, "error.unauthorized");
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Comment {CommentId} deleted by user {UserId}.", commentId, userId);
        return (true, "comment.delete.success");
    }
}