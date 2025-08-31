using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Sends a private message from one user to another.
    /// </summary>
    /// <param name="senderId">The ID of the user sending the message.</param>
    /// <param name="receiverId">The ID of the user receiving the message.</param>
    /// <param name="subject">The subject of the message.</param>
    /// <param name="content">The content of the message.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string Message)> SendMessageAsync(int senderId, SendMessageRequestDto request)
    {
        // 1. Validate sender and receiver existence.
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(request.ReceiverId);

        if (sender == null || receiver == null)
        {
            _logger.LogWarning("Message send failed: Sender {SenderId} or Receiver {ReceiverId} not found.", senderId, request.ReceiverId);
            return (false, "Sender or receiver not found.");
        }

        // Check if sender is banned from messaging
        if (sender.BanStatus.HasFlag(BanStatus.MessagingBan) || sender.BanStatus.HasFlag(BanStatus.LoginBan))
        {
            // Allow messaging only to administrators
            if (receiver.Role < UserRole.Moderator)
            {
                _logger.LogWarning("Banned user {SenderId} attempted to send a message to non-admin user {ReceiverId}.", senderId, request.ReceiverId);
                return (false, "You are banned from sending messages to this user.");
            }
        }

        // 2. Create a new Message entity.
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Subject = request.Subject,
            Content = request.Content,
            SentAt = DateTimeOffset.UtcNow,
            IsRead = false // New messages are unread by default.
        };

        // 3. Add the message to the database and save changes.
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Message sent from {SenderId} to {ReceiverId} with subject '{Subject}'.", senderId, request.ReceiverId, request.Subject);
        return (true, "Message sent successfully.");
    }

    public async Task<List<Message>> GetInboxAsync(int userId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == userId && !m.ReceiverDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<Message>> GetSentMessagesAsync(int userId)
    {
        return await _context.Messages
            .Where(m => m.SenderId == userId && !m.SenderDeleted)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message?> GetMessageAsync(int messageId, int userId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == messageId && (m.SenderId == userId || m.ReceiverId == userId));

        if (message != null && message.ReceiverId == userId && !message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return message;
    }

    public async Task<(bool Success, string Message)> MarkMessageAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);
        if (message == null)
        {
            return (false, "Message not found or you are not the receiver.");
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Message {MessageId} marked as read by user {UserId}.", messageId, userId);
        }
        return (true, "Message marked as read.");
    }

    /// <summary>
    /// Deletes a message for a specific user. Messages are soft-deleted (marked as deleted by sender/receiver).
    /// A message is permanently removed from the database only when both sender and receiver have deleted it.
    /// </summary>
    /// <param name="messageId">The ID of the message to delete.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    /// <param name="isSender">True if the user is the sender, false if the user is the receiver.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId)
    {
        // 1. Find the message by its ID.
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
        {
            return (false, "Message not found.");
        }

        // 2. Mark the message as deleted based on who is deleting it (sender or receiver).
        if (message.SenderId == userId)
        {
            message.SenderDeleted = true;
        }
        else if (message.ReceiverId == userId)
        {
            message.ReceiverDeleted = true;
        }
        else
        {
            // User is neither sender nor receiver.
            return (false, "You are not authorized to delete this message.");
        }

        // 3. If both sender and receiver have marked the message as deleted, permanently remove it from the database.
        if (message.SenderDeleted && message.ReceiverDeleted)
        {
            _context.Messages.Remove(message);
            _logger.LogInformation("Message {MessageId} permanently deleted from DB as both parties deleted it.", messageId);
        }

        // 4. Save changes to the database.
        await _context.SaveChangesAsync();
        _logger.LogInformation("Message {MessageId} marked as deleted by user {UserId}.", messageId, userId);
        return (true, "Message deleted successfully.");
    }
}

