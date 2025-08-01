using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Sakura.PT.Data;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMessageService _messageService;
    private readonly ILogger<AnnouncementService> _logger;
    private readonly IDistributedCache _cache;

    // Cache key for announcements
    private const string AnnouncementsCacheKey = "Announcements";
    // Cache duration (e.g., 1 hour)
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public AnnouncementService(ApplicationDbContext context, IMessageService messageService, ILogger<AnnouncementService> logger, IDistributedCache cache)
    {
        _context = context;
        _messageService = messageService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(string title, string content, int createdByUserId, bool sendToInbox)
    {
        var announcement = new Announcement
        {
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Announcement '{Title}' created by user {CreatedByUserId}.", title, createdByUserId);

        // Invalidate cache after creating a new announcement
        await _cache.RemoveAsync(AnnouncementsCacheKey);
        _logger.LogInformation("Announcements cache invalidated.");

        if (sendToInbox)
        {
            // Send to all active users via internal message system
            var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var userId in allUserIds)
            {
                // System message, senderId can be 0 or a dedicated system user ID
                await _messageService.SendMessageAsync(0, userId, $"Announcement: {title}", content);
            }
            _logger.LogInformation("Announcement '{Title}' sent to {UserCount} users via inbox.", title, allUserIds.Count);
        }

        return (true, "Announcement created successfully.", announcement);
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync()
    {
        var cachedData = await _cache.GetStringAsync(AnnouncementsCacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving announcements from cache.");
            return JsonSerializer.Deserialize<List<Announcement>>(cachedData) ?? new List<Announcement>();
        }

        _logger.LogInformation("Cache miss for announcements. Refreshing from DB.");
        var announcements = await _context.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        await _cache.SetStringAsync(AnnouncementsCacheKey, JsonSerializer.Serialize(announcements), _cacheOptions);
        return announcements;
    }
}

