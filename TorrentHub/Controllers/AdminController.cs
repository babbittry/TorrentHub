
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrator")]
public class AdminController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAdminService _adminService;

    public AdminController(ISettingsService settingsService, IAdminService adminService)
    {
        _settingsService = settingsService;
        _adminService = adminService;
    }

    [HttpGet("settings/site")]
    public async Task<ActionResult<SiteSettingsDto>> GetSiteSettings()
    {
        var settings = await _settingsService.GetSiteSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings/site")]
    public async Task<IActionResult> UpdateSiteSettings([FromBody] SiteSettingsDto dto)
    {
        await _settingsService.UpdateSiteSettingsAsync(dto);
        return Ok(new { message = "Site settings updated successfully." });
    }

    // Client Banning Endpoints

    [HttpGet("banned-clients")]
    public async Task<ActionResult<List<BannedClientDto>>> GetBannedClients()
    {
        var clients = await _adminService.GetBannedClientsAsync();
        // Manual mapping here, or could use a mapper
        var dtos = clients.Select(c => new BannedClientDto { Id = c.Id, UserAgentPrefix = c.UserAgentPrefix, Reason = c.Reason }).ToList();
        return Ok(dtos);
    }

    [HttpPost("banned-clients")]
    public async Task<ActionResult<BannedClientDto>> AddBannedClient([FromBody] BannedClientDto dto)
    {
        try
        {
            var newClient = await _adminService.AddBannedClientAsync(dto);
            var newDto = new BannedClientDto { Id = newClient.Id, UserAgentPrefix = newClient.UserAgentPrefix, Reason = newClient.Reason };
            return CreatedAtAction(nameof(GetBannedClients), new { id = newClient.Id }, newDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("banned-clients/{id:int}")]
    public async Task<IActionResult> DeleteBannedClient(int id)
    {
        var (success, message) = await _adminService.DeleteBannedClientAsync(id);
        if (!success)
        {
            return NotFound(new { message });
        }
        return NoContent();
    }

    // Duplicate IP Detection

    [HttpGet("duplicate-ips")]
    public async Task<ActionResult<List<DuplicateIpUserDto>>> GetDuplicateIps()
    {
        var result = await _adminService.GetDuplicateIpUsersAsync();
        return Ok(result);
    }

    // Cheat Logs

    [HttpGet("logs/cheat")]
    public async Task<ActionResult<List<CheatLogDto>>> GetCheatLogs()
    {
        var logs = await _adminService.GetCheatLogsAsync();
        return Ok(logs);
    }

    [HttpGet("logs/system")]
    public async Task<ActionResult<object>> SearchSystemLogs([FromQuery] LogSearchDto dto)
    {
        var results = await _adminService.SearchSystemLogsAsync(dto);
        return Ok(results);
    }

    [HttpGet("users")]
    public async Task<ActionResult<PaginatedResult<UserProfileDetailDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var users = await _adminService.GetUsersAsync(page, pageSize);
        return Ok(users);
    }
}
