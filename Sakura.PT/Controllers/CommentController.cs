using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/torrents/{torrentId}/[controller]")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly SakuraCoinSettings _settings;
    private readonly ILogger<CommentController> _logger;

    public CommentController(ApplicationDbContext context, IUserService userService, IOptions<SakuraCoinSettings> settings, ILogger<CommentController> logger)
    {
        _context = context;
        _userService = userService;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostComment(int torrentId, [FromBody] Comment newComment)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var today = DateTime.UtcNow.Date;

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return NotFound("Torrent not found.");
        }

        newComment.TorrentId = torrentId;
        newComment.UserId = userId;
        newComment.CreatedAt = DateTime.UtcNow;

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
            await _userService.AddSakuraCoinsAsync(userId, _settings.CommentBonus);
            _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId} and earned {Bonus} SakuraCoins. Daily count: {Count}", userId, torrentId, _settings.CommentBonus, dailyStats.CommentBonusesGiven);
        }
        else
        {
            _logger.LogInformation("User {UserId} posted a comment on torrent {TorrentId}. Daily bonus limit reached.", userId, torrentId);
        }

        await _context.SaveChangesAsync();

        return Ok(newComment);
    }
}
