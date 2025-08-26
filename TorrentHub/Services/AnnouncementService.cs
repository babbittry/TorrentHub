using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AnnouncementService> _logger;
    private readonly IDistributedCache _cache;

    private const string AnnouncementsCacheKey = "Announcements";
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public AnnouncementService(
        ApplicationDbContext context, 
        ILogger<AnnouncementService> logger, 
        IDistributedCache cache, 
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, int createdByUserId)
    {
        var announcement = new Announcement
        {
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Announcement '{Title}' created by user {CreatedByUserId}.", request.Title, createdByUserId);

        await _cache.RemoveAsync(AnnouncementsCacheKey);
        _logger.LogInformation("Announcements cache invalidated.");

        if (request.SendToInbox)
        {
            var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
            await _notificationService.SendNewAnnouncementNotificationAsync(announcement, allUserIds);
            _logger.LogInformation("Announcement '{Title}' queued for sending to {UserCount} users via inbox.", request.Title, allUserIds.Count);
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