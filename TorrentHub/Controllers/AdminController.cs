using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ISettingsService settingsService, ILogger<AdminController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpPut("settings/registration")]
    public async Task<IActionResult> UpdateRegistrationSettings([FromBody] UpdateRegistrationSettingsDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Convert boolean to lowercase string to store in DB
            var valueToStore = dto.IsOpen.ToString().ToLower();
            await _settingsService.SetSettingAsync("IsRegistrationOpen", valueToStore);
            
            _logger.LogInformation("Registration settings updated by {AdminUser}. IsRegistrationOpen set to {Status}", User.Identity?.Name, dto.IsOpen);

            return Ok(new { message = "Registration settings updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update registration settings.");
            return StatusCode(500, new { message = "An error occurred while updating settings." });
        }
    }
}
