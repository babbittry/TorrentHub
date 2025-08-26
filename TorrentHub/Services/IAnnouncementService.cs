using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IAnnouncementService
{
    Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, int createdByUserId);
    Task<List<Announcement>> GetAnnouncementsAsync();
    Task<(bool Success, string Message, Announcement? Announcement)> UpdateAnnouncementAsync(int announcementId, UpdateAnnouncementDto dto);
    Task<(bool Success, string Message)> DeleteAnnouncementAsync(int announcementId);
}
