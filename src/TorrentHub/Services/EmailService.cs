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
    private readonly IStringLocalizerFactory _localizerFactory;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger, IStringLocalizerFactory localizerFactory)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _localizerFactory = localizerFactory;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
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
        }
    }

    public async Task SendVerificationCodeAsync(User user, string code, string purpose, int expiresInMinutes)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            _logger.LogDebug("Sending verification code email. User language: {UserLanguage}", user.Language);

            // Set the culture for this thread
            var culture = new CultureInfo(user.Language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Create a culture-specific localizer
            var localizer = _localizerFactory.Create(typeof(Messages).Name, typeof(Messages).Assembly.GetName().Name!);

            _logger.LogDebug("Using culture: {Culture}", culture.Name);

            var subject = localizer["Email_VerificationCode_Subject", _smtpSettings.FromName];
            var purposeLocalized = localizer[purpose]; // Assuming purpose is a resource key like "LoginVerification"

            _logger.LogDebug("Localized subject: '{LocalizedSubject}'", subject);
            _logger.LogDebug("Localized purpose: '{LocalizedPurpose}'", purposeLocalized);

            var message = $"""
                <p>{localizer["Email_Greeting", user.UserName]}</p>
                <p>{localizer["Email_VerificationCode_Body", purposeLocalized]}</p>
                <h2 style="font-weight:bold; letter-spacing: 2px;">{code}</h2>
                <p>{localizer["Email_CodeExpires_Body", expiresInMinutes]}</p>
                <p>{localizer["Email_IgnoreIfNotRequested_Body"]}</p>
                """;

            await SendEmailAsync(user.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification code for {UserName}", user.UserName);
            throw;
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    public async Task SendEmailVerificationLinkAsync(User user, string verificationToken)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            // Set the culture for this thread
            var culture = new CultureInfo(user.Language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var localizer = _localizerFactory.Create(typeof(Messages).Name, typeof(Messages).Assembly.GetName().Name!);

            var subject = localizer["Email_VerifyAddress_Subject", _smtpSettings.FromAddress];
            var verificationLink = $"https://localhost:7122/api/auth/verify-email?token={verificationToken}";

            var message = $"""
                <p>{localizer["Email_Greeting", user.UserName]}</p>
                <p>{localizer["Email_VerifyAddress_Body"]}</p>
                <p><a href="{verificationLink}">{localizer["Email_VerifyAddress_LinkText"]}</a></p>
                <p>{localizer["Email_IgnoreIfNotRequested_Body"]}</p>
                """;

            await SendEmailAsync(user.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification link for {UserName}", user.UserName);
            throw;
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
