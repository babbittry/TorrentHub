using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TorrentHub.Core.Entities;
using TorrentHub.Services.Configuration;
using TorrentHub.Services.Interfaces;
using Microsoft.Extensions.Localization;
using TorrentHub.Resources;
using System.Globalization;

namespace TorrentHub.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IStringLocalizer<Messages> _localizer;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger, IStringLocalizer<Messages> localizer)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_smtpSettings.FromAddress);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
            await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {ToEmail} with subject {Subject}.", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}.", toEmail, subject);
            throw;
        }
    }
    
    private void SetCulture(string language)
    {
        try
        {
            var cultureInfo = new CultureInfo(language);
            CultureInfo.CurrentUICulture = cultureInfo;
        }
        catch (CultureNotFoundException)
        {
            _logger.LogWarning("Could not set culture to '{Language}', language not found. Falling back to default.", language);
        }
    }

    public async Task SendVerificationCodeAsync(User user, string code, string purpose, int expiresInMinutes)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            SetCulture(user.Language);

            var subject = _localizer["Email_VerificationCode_Subject", _smtpSettings.FromAddress];
            var purposeLocalized = _localizer[purpose]; // Assuming purpose is a resource key like "LoginVerification"
            var message = $"""
                <p>{_localizer["Email_Greeting", user.UserName]}</p>
                <p>{_localizer["Email_VerificationCode_Body", purposeLocalized]}</p>
                <h2 style="font-weight:bold; letter-spacing: 2px;">{code}</h2>
                <p>{_localizer["Email_CodeExpires_Body", expiresInMinutes]}</p>
                <p>{_localizer["Email_IgnoreIfNotRequested_Body"]}</p>
                """;

            await SendEmailAsync(user.Email, subject, message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    public async Task SendEmailVerificationLinkAsync(User user, string verificationToken)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            SetCulture(user.Language);
            var subject = _localizer["Email_VerifyAddress_Subject", _smtpSettings.FromAddress];
            var verificationLink = $"https://localhost:7122/api/auth/verify-email?token={verificationToken}"; 

            var message = $"""
                <p>{_localizer["Email_Greeting", user.UserName]}</p>
                <p>{_localizer["Email_VerifyAddress_Body"]}</p>
                <p><a href="{verificationLink}">{_localizer["Email_VerifyAddress_LinkText"]}</a></p>
                <p>{_localizer["Email_IgnoreIfNotRequested_Body"]}</p>
                """;

            await SendEmailAsync(user.Email, subject, message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
