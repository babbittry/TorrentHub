using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Resources;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class NotificationService : INotificationService
{
    private readonly IMessageService _messageService;
    private readonly IEmailService _emailService;
    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly ApplicationDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IMessageService messageService,
        IEmailService emailService,
        IStringLocalizerFactory localizerFactory,
        ApplicationDbContext context,
        ILogger<NotificationService> logger,
        ISettingsService settingsService)
    {
        _messageService = messageService;
        _emailService = emailService;
        _localizerFactory = localizerFactory;
        _context = context;
        _logger = logger;
        _settingsService = settingsService;
    }

    private async Task SendLocalizedMessage(int recipientId, string subjectKey, string contentKey, params object[] args)
    {
        var user = await _context.Users.FindAsync(recipientId);
        if (user == null)
        {
            _logger.LogWarning("Cannot send localized message to non-existent user {UserId}", recipientId);
            return;
        }

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = new CultureInfo(user.Language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var localizer = _localizerFactory.Create(typeof(Messages).Name, typeof(Messages).Assembly.GetName().Name!);

            var subject = localizer[subjectKey, args];
            var content = localizer[contentKey, args];

            await _messageService.SendMessageAsync(0, new SendMessageRequestDto { ReceiverId = recipientId, Subject = subject, Content = content });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    public async Task SendRequestFilledNotificationAsync(Request request)
    {
        if (request.RequestedByUser == null || request.FilledByUser == null || request.FilledWithTorrent == null)
        {
            _logger.LogWarning("Cannot send RequestFilled notification for RequestId {RequestId} due to missing navigation properties.", request.Id);
            return;
        }
        await SendLocalizedMessage(request.RequestedByUserId, 
            "RequestFilled_Subject", 
            "RequestFilled_Content", 
            request.FilledByUser.UserName, request.Title, request.FilledWithTorrent.Name, request.FilledWithTorrent.Id);
    }

    public async Task SendRequestConfirmedNotificationAsync(Request request)
    {
        if (request.FilledByUser == null || !request.FilledByUserId.HasValue)
        {
            _logger.LogWarning("Cannot send RequestConfirmed notification for RequestId {RequestId} due to missing user data.", request.Id);
            return;
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        var bonus = settings.FillRequestBonus + request.BountyAmount;
        await SendLocalizedMessage(request.FilledByUserId.Value, 
            "RequestConfirmed_Subject", 
            "RequestConfirmed_Content", 
            request.Title, bonus);
    }

    public async Task SendRequestRejectedNotificationAsync(Request request)
    {
        if (request.FilledByUser == null || !request.FilledByUserId.HasValue)
        {
            _logger.LogWarning("Cannot send RequestRejected notification for RequestId {RequestId} due to missing user data.", request.Id);
            return;
        }

        await SendLocalizedMessage(request.FilledByUserId.Value, 
            "RequestRejected_Subject", 
            "RequestRejected_Content", 
            request.Title, request.RejectionReason ?? "N/A");
    }

    public async Task SendWelcomeEmailAsync(User user)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = new CultureInfo(user.Language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var localizer = _localizerFactory.Create(typeof(Messages).Name, typeof(Messages).Assembly.GetName().Name!);

            var subject = localizer["Welcome_Subject"];
            var body = localizer["Welcome_Content", user.UserName];
            await _emailService.SendEmailAsync(user.Email, subject, body);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    public async Task SendNewAnnouncementNotificationAsync(Announcement announcement, IEnumerable<int> userIds)
    {
        _logger.LogInformation("Sending announcement {AnnouncementId} to {UserCount} users.", announcement.Id, userIds.Count());

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

        foreach (var user in users)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = new CultureInfo(user.Language);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var localizer = _localizerFactory.Create(typeof(Messages).Name, typeof(Messages).Assembly.GetName().Name!);

                var subject = localizer["Announcement_Subject", announcement.Title];
                var content = localizer["Announcement_Content", announcement.Content];

                await _messageService.SendMessageAsync(0, new SendMessageRequestDto { ReceiverId = user.Id, Subject = subject, Content = content });
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}

