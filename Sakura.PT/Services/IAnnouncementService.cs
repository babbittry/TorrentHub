using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface IAnnouncementService
{
    Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(string title, string content, int createdByUserId, bool sendToInbox);
    Task<List<Announcement>> GetAnnouncementsAsync();
}
