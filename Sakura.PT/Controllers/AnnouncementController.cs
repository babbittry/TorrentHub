using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly ILogger<AnnouncementController> _logger;

    public AnnouncementController(IAnnouncementService announcementService, ILogger<AnnouncementController> logger)
    {
        _announcementService = announcementService;
        _logger = logger;
    }

    [HttpPost("create")]
    [Authorize(Roles = "Administrator,Moderator")] // Only admins/mods can create announcements
    public async Task<IActionResult> CreateAnnouncement([FromForm] string title, [FromForm] string content, [FromForm] bool sendToInbox = false)
    {
        var createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, announcement) = await _announcementService.CreateAnnouncementAsync(title, content, createdByUserId, sendToInbox);

        if (!success)
        {
            _logger.LogWarning("Failed to create announcement: {Message}", message);
            return BadRequest(message);
        }

        return Ok(announcement);
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements()
    {
        var announcements = await _announcementService.GetAnnouncementsAsync();
        return Ok(announcements);
    }
}
