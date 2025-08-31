using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/announcements")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly ILogger<AnnouncementsController> _logger;

    public AnnouncementsController(IAnnouncementService announcementService, ILogger<AnnouncementsController> logger)
    {
        _announcementService = announcementService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Moderator")] // Only admins/mods can create announcements
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement([FromBody] CreateAnnouncementRequestDto request)
    {
        var createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, announcement) = await _announcementService.CreateAnnouncementAsync(request, createdByUserId);

        if (!success)
        {
            _logger.LogWarning("Failed to create announcement: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(Mapper.ToAnnouncementDto(announcement!));
    }

    [HttpGet]
    public async Task<ActionResult<List<AnnouncementDto>>> GetAnnouncements()
    {
        var announcements = await _announcementService.GetAnnouncementsAsync();
        return Ok(announcements.Select(a => Mapper.ToAnnouncementDto(a)).ToList());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<ActionResult<AnnouncementDto>> UpdateAnnouncement(int id, [FromBody] UpdateAnnouncementDto dto)
    {
        var (success, message, announcement) = await _announcementService.UpdateAnnouncementAsync(id, dto);

        if (!success)
        {
            return NotFound(new { message });
        }

        return Ok(Mapper.ToAnnouncementDto(announcement!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var (success, message) = await _announcementService.DeleteAnnouncementAsync(id);

        if (!success)
        {
            return NotFound(new { message });
        }

        return NoContent();
    }
}
