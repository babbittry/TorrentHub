using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto request)
    {
        var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.SendMessageAsync(senderId, request);

        if (!success)
        {
            _logger.LogWarning("Failed to send message: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<List<MessageDto>>> GetInbox()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetInboxAsync(userId);
        return Ok(messages.Select(m => Mapper.ToMessageDto(m)).ToList());
    }

    [HttpGet("sent")]
    public async Task<ActionResult<List<MessageDto>>> GetSentMessages()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetSentMessagesAsync(userId);
        return Ok(messages.Select(m => Mapper.ToMessageDto(m)).ToList());
    }

    [HttpGet("{messageId:int}")]
    public async Task<ActionResult<MessageDto>> GetMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var message = await _messageService.GetMessageAsync(messageId, userId);

        if (message == null)
        {
            return NotFound(new { message = "Message not found or you are not authorized to view it." });
        }

        return Ok(Mapper.ToMessageDto(message));
    }

    [HttpPatch("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.MarkMessageAsReadAsync(messageId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to mark message {MessageId} as read: {ErrorMessage}", messageId, message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }

    [HttpDelete("{messageId:int}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.DeleteMessageAsync(messageId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to delete message {MessageId}: {ErrorMessage}", messageId, message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }
}
