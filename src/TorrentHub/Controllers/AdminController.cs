
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

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
    public async Task<ActionResult<ApiResponse<SiteSettingsDto>>> GetSiteSettings()
    {
        var settings = await _settingsService.GetSiteSettingsAsync();
        return Ok(ApiResponse<SiteSettingsDto>.SuccessResult(settings));
    }

    [HttpPut("settings/site")]
    public async Task<ActionResult<ApiResponse>> UpdateSiteSettings([FromBody] SiteSettingsDto dto)
    {
        await _settingsService.UpdateSiteSettingsAsync(dto);
        return Ok(ApiResponse.SuccessResult("Site settings updated successfully."));
    }

    // Client Banning Endpoints

    [HttpGet("banned-clients")]
    public async Task<ActionResult<ApiResponse<List<BannedClientDto>>>> GetBannedClients()
    {
        var clients = await _adminService.GetBannedClientsAsync();
        // Manual mapping here, or could use a mapper
        var dtos = clients.Select(c => new BannedClientDto { Id = c.Id, UserAgentPrefix = c.UserAgentPrefix, Reason = c.Reason }).ToList();
        return Ok(ApiResponse<List<BannedClientDto>>.SuccessResult(dtos));
    }

    [HttpPost("banned-clients")]
    public async Task<ActionResult<ApiResponse<BannedClientDto>>> AddBannedClient([FromBody] BannedClientDto dto)
    {
        try
        {
            var newClient = await _adminService.AddBannedClientAsync(dto);
            var newDto = new BannedClientDto { Id = newClient.Id, UserAgentPrefix = newClient.UserAgentPrefix, Reason = newClient.Reason };
            return CreatedAtAction(nameof(GetBannedClients), new { id = newClient.Id }, ApiResponse<BannedClientDto>.SuccessResult(newDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BannedClientDto>.ErrorResult(ex.Message));
        }
    }

    [HttpDelete("banned-clients/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteBannedClient(int id)
    {
        var (success, message) = await _adminService.DeleteBannedClientAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.ErrorResult(message));
        }
        return Ok(ApiResponse.SuccessResult("Banned client deleted successfully."));
    }

    // Duplicate IP Detection

    [HttpGet("duplicate-ips")]
    public async Task<ActionResult<ApiResponse<List<DuplicateIpUserDto>>>> GetDuplicateIps()
    {
        var result = await _adminService.GetDuplicateIpUsersAsync();
        return Ok(ApiResponse<List<DuplicateIpUserDto>>.SuccessResult(result));
    }

    // Cheat Logs

    [HttpGet("logs/cheat")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CheatLogDto>>>> GetCheatLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? userId = null,
        [FromQuery] string? detectionType = null)
    {
        var logs = await _adminService.GetCheatLogsAsync(page, pageSize, userId, detectionType);
        return Ok(ApiResponse<PaginatedResult<CheatLogDto>>.SuccessResult(logs));
    }

    // CheatLog 处理状态管理
    
    /// <summary>
    /// 标记作弊日志为已处理
    /// </summary>
    [HttpPost("logs/cheat/{id}/process")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<ActionResult<ApiResponse<CheatLogDto>>> ProcessCheatLog(
        int id,
        [FromBody] ProcessCheatLogRequest request)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var success = await _adminService.MarkCheatLogAsProcessedAsync(id, adminUserId, request.Notes);
            
            if (!success)
            {
                return NotFound(ApiResponse<CheatLogDto>.ErrorResult("Cheat log not found"));
            }

            // 重新获取更新后的日志
            var logs = await _adminService.GetCheatLogsAsync(1, 1, null, null);
            var updatedLog = logs.Items.FirstOrDefault();

            if (updatedLog == null)
            {
                return NotFound(ApiResponse<CheatLogDto>.ErrorResult("Unable to retrieve updated cheat log"));
            }

            return Ok(ApiResponse<CheatLogDto>.SuccessResult(updatedLog, "Cheat log marked as processed"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<CheatLogDto>.ErrorResult($"Error processing cheat log: {ex.Message}"));
        }
    }

    /// <summary>
    /// 批量标记作弊日志为已处理
    /// </summary>
    [HttpPost("logs/cheat/process-batch")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<ActionResult<ApiResponse<BatchProcessResponse>>> ProcessCheatLogsBatch(
        [FromBody] BatchProcessCheatLogsRequest request)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _adminService.MarkCheatLogsBatchAsProcessedAsync(
                request.LogIds, 
                adminUserId, 
                request.Notes);

            return Ok(ApiResponse<BatchProcessResponse>.SuccessResult(
                new BatchProcessResponse
                {
                    ProcessedCount = count,
                    TotalRequested = request.LogIds.Length
                },
                $"Successfully processed {count} cheat logs"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BatchProcessResponse>.ErrorResult($"Error batch processing: {ex.Message}"));
        }
    }

    /// <summary>
    /// 取消作弊日志的处理状态
    /// </summary>
    [HttpPost("logs/cheat/{id}/unprocess")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> UnprocessCheatLog(int id)
    {
        try
        {
            var success = await _adminService.UnmarkCheatLogAsync(id);
            
            if (!success)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Cheat log not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Cheat log processing status removed"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error unprocessing: {ex.Message}"));
        }
    }

    [HttpGet("logs/system")]
    public async Task<ActionResult<ApiResponse<List<JsonDocument>>>> SearchSystemLogs([FromQuery] LogSearchDto dto)
    {
        var results = await _adminService.SearchSystemLogsAsync(dto);
        return Ok(ApiResponse<List<JsonDocument>>.SuccessResult(results));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserAdminProfileDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var users = await _adminService.GetUsersAsync(page, pageSize);
        return Ok(ApiResponse<PaginatedResult<UserAdminProfileDto>>.SuccessResult(users));
    }
}

