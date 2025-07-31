using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMessageService _messageService;
    private readonly ILogger<AnnouncementService> _logger;

    public AnnouncementService(ApplicationDbContext context, IMessageService messageService, ILogger<AnnouncementService> logger)
    {
        _context = context;
        _messageService = messageService;
        _logger = logger;
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
        return await _context.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
