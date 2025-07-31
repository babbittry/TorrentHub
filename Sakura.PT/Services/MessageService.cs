using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendMessageAsync(int senderId, int receiverId, string subject, string content)
    {
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);

        if (sender == null || receiver == null)
        {
            _logger.LogWarning("Message send failed: Sender {SenderId} or Receiver {ReceiverId} not found.", senderId, receiverId);
            return (false, "Sender or receiver not found.");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Subject = subject,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Message sent from {SenderId} to {ReceiverId} with subject '{Subject}'.", senderId, receiverId, subject);
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

    public async Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId, bool isSender)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
        {
            return (false, "Message not found.");
        }

        if (isSender && message.SenderId == userId)
        {
            message.SenderDeleted = true;
        }
        else if (!isSender && message.ReceiverId == userId)
        {
            message.ReceiverDeleted = true;
        }
        else
        {
            return (false, "You are not authorized to delete this message.");
        }

        // If both sender and receiver have deleted the message, remove it from DB
        if (message.SenderDeleted && message.ReceiverDeleted)
        {
            _context.Messages.Remove(message);
            _logger.LogInformation("Message {MessageId} permanently deleted from DB.", messageId);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Message {MessageId} marked as deleted by user {UserId} (isSender: {IsSender}).", messageId, userId, isSender);
        return (true, "Message deleted successfully.");
    }
}
