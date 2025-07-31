using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface IMessageService
{
    Task<(bool Success, string Message)> SendMessageAsync(int senderId, int receiverId, string subject, string content);
    Task<List<Message>> GetInboxAsync(int userId);
    Task<List<Message>> GetSentMessagesAsync(int userId);
    Task<Message?> GetMessageAsync(int messageId, int userId);
    Task<(bool Success, string Message)> MarkMessageAsReadAsync(int messageId, int userId);
    Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId, bool isSender);
}
