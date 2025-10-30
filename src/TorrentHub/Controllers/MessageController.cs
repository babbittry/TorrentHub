using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

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
    public async Task<ActionResult<ApiResponse<object>>> SendMessage([FromBody] SendMessageRequestDto request)
    {
        var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.SendMessageAsync(senderId, request);

        if (!success)
        {
            _logger.LogWarning("Failed to send message: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetInbox()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetInboxAsync(userId);
        return Ok(new ApiResponse<List<MessageDto>>
        {
            Success = true,
            Data = messages.Select(m => Mapper.ToMessageDto(m)).ToList(),
            Message = "Inbox retrieved successfully."
        });
    }

    [HttpGet("sent")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetSentMessages()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var messages = await _messageService.GetSentMessagesAsync(userId);
        return Ok(new ApiResponse<List<MessageDto>>
        {
            Success = true,
            Data = messages.Select(m => Mapper.ToMessageDto(m)).ToList(),
            Message = "Sent messages retrieved successfully."
        });
    }

    [HttpGet("{messageId:int}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> GetMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var message = await _messageService.GetMessageAsync(messageId, userId);

        if (message == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Message not found or you are not authorized to view it."
            });
        }

        return Ok(new ApiResponse<MessageDto>
        {
            Success = true,
            Data = Mapper.ToMessageDto(message),
            Message = "Message retrieved successfully."
        });
    }

    [HttpPatch("{messageId}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.MarkMessageAsReadAsync(messageId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to mark message {MessageId} as read: {ErrorMessage}", messageId, message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    [HttpDelete("{messageId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _messageService.DeleteMessageAsync(messageId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to delete message {MessageId}: {ErrorMessage}", messageId, message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }
}
