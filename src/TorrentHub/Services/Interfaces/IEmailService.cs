namespace TorrentHub.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendVerificationCodeAsync(TorrentHub.Core.Entities.User user, string code, string purpose, int expiresInMinutes);
    Task SendEmailVerificationLinkAsync(TorrentHub.Core.Entities.User user, string verificationToken);
}
