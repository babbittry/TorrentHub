using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Sakura.PT.Services;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;

namespace Sakura.PT.Controllers;

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
    public async Task<ActionResult<IEnumerable<InviteDto>>> GetMyInvites()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        var invites = await _userService.GetUserInvitesAsync(userId);
        return Ok(invites.Select(Mapper.ToInviteDto));
    }

    [HttpPost]
    public async Task<ActionResult<InviteDto>> GenerateInvite()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var newInvite = await _userService.GenerateInviteAsync(userId);
            return Ok(Mapper.ToInviteDto(newInvite));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invite for user {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }
}
