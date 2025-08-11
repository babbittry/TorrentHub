using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.DTOs;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly SakuraCoinSettings _settings;
    private readonly ILogger<CommentService> _logger;

    public CommentService(ApplicationDbContext context, IUserService userService, IOptions<SakuraCoinSettings> settings, ILogger<CommentService> logger)
    {
        _context = context;
        _userService = userService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Comment? Comment)> PostCommentAsync(int torrentId, CreateCommentRequestDto request, int userId)
    {
        var today = DateTime.UtcNow.Date;

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.", null);
        }

        var newComment = new Comment
        {
            TorrentId = torrentId,
            UserId = userId,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(newComment);

        // Check for daily bonus limit
        var dailyStats = await _context.UserDailyStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == today);

        if (dailyStats == null)
        {
            dailyStats = new UserDailyStats { UserId = userId, Date = today };
            _context.UserDailyStats.Add(dailyStats);
        }

        if (dailyStats.CommentBonusesGiven < _settings.MaxDailyCommentBonuses)
        {
            dailyStats.CommentBonusesGiven++;
            await _userService.AddSakuraCoinsAsync(userId, new UpdateSakuraCoinsRequestDto { Amount = _settings.CommentBonus });
            _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId} and earned {Bonus} SakuraCoins. Daily count: {Count}", userId, torrentId, _settings.CommentBonus, dailyStats.CommentBonusesGiven);
        }
        else
        {
            _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId}. Daily bonus limit reached.", userId, torrentId);
        }

        await _context.SaveChangesAsync();

        return (true, "Comment posted successfully.", newComment);
    }

    public async Task<IEnumerable<Comment>> GetCommentsForTorrentAsync(int torrentId, int page, int pageSize)
    {
        return await _context.Comments
            .Where(c => c.TorrentId == torrentId)
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.User) // Include user information
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> UpdateCommentAsync(int commentId, UpdateCommentRequestDto request, int userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "Comment not found.");
        }

        // Only the owner or an admin can update the comment
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || (comment.UserId != userId && user.Role != UserRole.Administrator))
        {
            return (false, "Unauthorized to update this comment.");
        }

        comment.Text = request.Content;
        comment.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Comment {CommentId} updated by user {UserId}.", commentId, userId);
        return (true, "Comment updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, "Comment not found.");
        }

        // Only the owner or an admin can delete the comment
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || (comment.UserId != userId && user.Role != UserRole.Administrator))
        {
            return (false, "Unauthorized to delete this comment.");
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Comment {CommentId} deleted by user {UserId}.", commentId, userId);
        return (true, "Comment deleted successfully.");
    }
}
