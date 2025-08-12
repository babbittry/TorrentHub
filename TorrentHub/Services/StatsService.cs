using Microsoft.EntityFrameworkCore;
using TorrentHub.Data;

namespace TorrentHub.Services;

public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _context;

    public StatsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetSiteStatsAsync()
    {
        var userCount = await _context.Users.CountAsync();
        var torrentCount = await _context.Torrents.CountAsync();
        var commentCount = await _context.Comments.CountAsync();
        var requestCount = await _context.Requests.CountAsync();

        return new
        {
            UserCount = userCount,
            TorrentCount = torrentCount,
            CommentCount = commentCount,
            RequestCount = requestCount
        };
    }
}
