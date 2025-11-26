using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/invites")]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<InvitesController> _logger;

    public InvitesController(IUserService userService, ILogger<InvitesController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<InviteDto>>>> GetMyInvites()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        var invites = await _userService.GetUserInvitesAsync(userId);
        return Ok(ApiResponse<List<InviteDto>>.SuccessResult(invites.Select(Mapper.ToInviteDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InviteDto>>> GenerateInvite()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var newInvite = await _userService.GenerateInviteAsync(userId);
            return Ok(ApiResponse<InviteDto>.SuccessResult(Mapper.ToInviteDto(newInvite)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invite for user {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(ApiResponse<InviteDto>.ErrorResult(ex.Message));
        }
    }
}

