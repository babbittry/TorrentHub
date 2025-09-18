using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Gets a collection of public site settings.
    /// This endpoint is available to all authenticated users.
    /// </summary>
    [HttpGet("public")]
    [Authorize]
    public async Task<ActionResult<PublicSiteSettingsDto>> GetPublicSettings()
    {
        var settings = await _settingsService.GetPublicSiteSettingsAsync();
        return Ok(settings);
    }
}