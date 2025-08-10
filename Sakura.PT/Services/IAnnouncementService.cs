using Sakura.PT.Entities;
using Sakura.PT.DTOs;

namespace Sakura.PT.Services;

public interface IAnnouncementService
{
    Task<(bool Success, string Message, Announcement? Announcement)> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, int createdByUserId);
    Task<List<Announcement>> GetAnnouncementsAsync();
}
