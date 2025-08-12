using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IAnnouncementService
{
    Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, int createdByUserId);
    Task<List<Announcement>> GetAnnouncementsAsync();
}
