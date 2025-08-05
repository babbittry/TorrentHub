using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMessageService messageService, ILogger<MessageController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromForm] int receiverId, [FromForm] string subject, [FromForm] string content)
    {
        var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.SendMessageAsync(senderId, receiverId, subject, content);

        if (!success)
        {
            _logger.LogWarning("Failed to send message: {Message}", message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }

    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetInboxAsync(userId);
        return Ok(messages);
    }

    [HttpGet("sent")]
    public async Task<IActionResult> GetSentMessages()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetSentMessagesAsync(userId);
        return Ok(messages);
    }

    [HttpGet("{messageId}")]
    public async Task<IActionResult> GetMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var message = await _messageService.GetMessageAsync(messageId, userId);

        if (message == null)
        {
            return NotFound("Message not found or you are not authorized to view it.");
        }

        return Ok(message);
    }

    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.MarkMessageAsReadAsync(messageId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to mark message {MessageId} as read: {ErrorMessage}", messageId, message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }

    [HttpDelete("{messageId}/sender")]
    public async Task<IActionResult> DeleteMessageAsSender(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.DeleteMessageAsync(messageId, userId, true);

        if (!success)
        {
            _logger.LogWarning("Failed to delete message {MessageId} as sender: {ErrorMessage}", messageId, message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }

    [HttpDelete("{messageId}/receiver")]
    public async Task<IActionResult> DeleteMessageAsReceiver(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.DeleteMessageAsync(messageId, userId, false);

        if (!success)
        {
            _logger.LogWarning("Failed to delete message {MessageId} as receiver: {ErrorMessage}", messageId, message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }
}
