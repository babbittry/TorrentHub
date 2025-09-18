
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Notifies a user that their request has been filled and is pending confirmation.
    /// </summary>
    Task SendRequestFilledNotificationAsync(Request request);

    /// <summary>
    /// Notifies a user that their fulfillment of a request has been confirmed.
    /// </summary>
    Task SendRequestConfirmedNotificationAsync(Request request);

    /// <summary>
    /// Notifies a user that their fulfillment of a request has been rejected.
    /// </summary>
    Task SendRequestRejectedNotificationAsync(Request request);

    /// <summary>
    /// Sends a welcome email to a newly registered user.
    /// </summary>
    Task SendWelcomeEmailAsync(User user);

    /// <summary>
    /// Sends a new announcement as a private message to a list of users.
    /// </summary>
    Task SendNewAnnouncementNotificationAsync(Announcement announcement, IEnumerable<int> userIds);
    
    /// <summary>
    /// Notifies a user about a coin transaction.
    /// </summary>
    Task SendCoinTransactionNotificationAsync(int toUserId, ulong receivedAmount, TransactionType type, string fromUsername);
}

