
#nullable enable
using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

/// <summary>
/// Service class containing the business logic for managing requests (bounties).
/// </summary>
public class RequestService : IRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<RequestService> _logger;

    public RequestService(
        ApplicationDbContext context, 
        IUserService userService, 
        ILogger<RequestService> logger, 
        INotificationService notificationService,
        ISettingsService settingsService)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
        _notificationService = notificationService;
        _settingsService = settingsService;
    }

    public async Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var settings = await _settingsService.GetSiteSettingsAsync();

        if (!settings.IsRequestSystemEnabled)
        {
            return (false, "error.request.disabled", null);
        }

        if (user == null)
        {
            return (false, "error.user.notFound", null);
        }

        if (createRequestDto.InitialBounty > 0 && user.Coins < createRequestDto.InitialBounty)
        {
            return (false, "error.request.insufficientCoins", null);
        }

        var newRequest = new Request
        {
            Title = createRequestDto.Title,
            Description = createRequestDto.Description,
            RequestedByUserId = userId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            BountyAmount = createRequestDto.InitialBounty
        };

        if (createRequestDto.InitialBounty > 0)
        {
            user.Coins -= createRequestDto.InitialBounty;
        }

        _context.Requests.Add(newRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created a new request titled '{RequestTitle}' with initial bounty {BountyAmount}.", userId, newRequest.Title, newRequest.BountyAmount);
        return (true, "request.create.success", newRequest);
    }

    public async Task<(bool Success, string Message)> AddBountyAsync(int requestId, AddBountyRequestDto addBountyRequestDto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "error.user.notFound");
        }

        if (addBountyRequestDto.Amount <= 0)
        {
            return (false, "error.request.bountyMustBePositive");
        }

        if (user.Coins < addBountyRequestDto.Amount)
        {
            return (false, "error.request.insufficientCoins");
        }

        var request = await _context.Requests.FindAsync(requestId);
        if (request == null || (request.Status != RequestStatus.Pending && request.Status != RequestStatus.Rejected))
        {
            return (false, "error.request.notFoundOrCannotAddBounty");
        }

        user.Coins -= addBountyRequestDto.Amount;
        request.BountyAmount += addBountyRequestDto.Amount;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added {Amount} Coins to request {RequestId}. New bounty: {NewBounty}.", userId, addBountyRequestDto.Amount, requestId, request.BountyAmount);
        return (true, "request.addBounty.success");
    }

    public async Task<List<Request>> GetRequestsAsync(RequestStatus? status, string sortBy, string sortOrder)
    {
        var query = _context.Requests.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        query = sortBy.ToLower() switch
        {
            "bountyamount" => isAscending 
                ? query.OrderBy(r => r.BountyAmount) 
                : query.OrderByDescending(r => r.BountyAmount),
            "createdat" => isAscending 
                ? query.OrderBy(r => r.CreatedAt) 
                : query.OrderByDescending(r => r.CreatedAt),
            "status" => isAscending 
                ? query.OrderBy(r => r.Status) 
                : query.OrderByDescending(r => r.Status),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        return await query
            .Include(r => r.RequestedByUser)
            .Include(r => r.FilledByUser)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> FillRequestAsync(int requestId, FillRequestDto fillRequestDto, int fillerUserId)
    {
        var request = await _context.Requests
            .Include(r => r.RequestedByUser)
            .Include(r => r.FilledWithTorrent)
            .FirstOrDefaultAsync(r => r.Id == requestId);
            
        if (request == null || (request.Status != RequestStatus.Pending && request.Status != RequestStatus.Rejected))
        {
            return (false, "error.request.notFoundOrProcessing");
        }

        if (request.RequestedByUserId == fillerUserId)
        {
            return (false, "error.request.cannotFillOwn");
        }

        var torrent = await _context.Torrents.FindAsync(fillRequestDto.TorrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound");
        }

        var fillerUser = await _context.Users.FindAsync(fillerUserId);
        if (fillerUser == null)
        {
            return (false, "error.user.notFound");
        }

        request.Status = RequestStatus.PendingConfirmation;
        request.FilledWithTorrentId = fillRequestDto.TorrentId;
        request.FilledByUserId = fillerUserId;
        request.FilledAt = DateTimeOffset.UtcNow;
        request.ConfirmationDeadline = DateTimeOffset.UtcNow.AddHours(72);
        request.RejectionReason = null;
        request.FilledByUser = fillerUser;
        request.FilledWithTorrent = torrent;

        await _notificationService.SendRequestFilledNotificationAsync(request);

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {FillerUserId} filled request {RequestId}. It is now pending confirmation from user {RequesterId}.", fillerUserId, requestId, request.RequestedByUserId);

        return (true, "request.fill.success");
    }

    public async Task<(bool Success, string Message)> ConfirmFulfillmentAsync(int requestId, int userId)
    {
        var request = await _context.Requests
            .Include(r => r.FilledByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            return (false, "error.request.notFound");
        }

        if (request.RequestedByUserId != userId)
        {
            return (false, "error.request.notRequester");
        }

        if (request.Status != RequestStatus.PendingConfirmation)
        {
            return (false, "error.request.notAwaitingConfirmation");
        }

        await CompleteRequestAsync(request);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} confirmed fulfillment for request {RequestId}.", userId, requestId);
        return (true, "request.confirm.success");
    }

    public async Task<(bool Success, string Message)> RejectFulfillmentAsync(int requestId, RejectFulfillmentDto rejectDto, int userId)
    {
        var request = await _context.Requests
            .Include(r => r.FilledByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            return (false, "error.request.notFound");
        }

        if (request.RequestedByUserId != userId)
        {
            return (false, "error.request.notRequester");
        }

        if (request.Status != RequestStatus.PendingConfirmation)
        {
            return (false, "error.request.notAwaitingConfirmation");
        }

        await _notificationService.SendRequestRejectedNotificationAsync(request);

        request.Status = RequestStatus.Rejected;
        request.RejectionReason = rejectDto.Reason;
        request.FilledByUserId = null;
        request.FilledWithTorrentId = null;
        request.FilledAt = null;
        request.ConfirmationDeadline = null;

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} rejected fulfillment for request {RequestId} with reason: {Reason}", userId, requestId, rejectDto.Reason);

        return (true, "request.reject.success");
    }

    public async Task AutoCompleteExpiredConfirmationsAsync()
    {
        var expiredRequests = await _context.Requests
            .Include(r => r.FilledByUser)
            .Where(r => r.Status == RequestStatus.PendingConfirmation && r.ConfirmationDeadline < DateTimeOffset.UtcNow)
            .ToListAsync();

        foreach (var request in expiredRequests)
        {
            _logger.LogInformation("Auto-completing request {RequestId} due to expired confirmation deadline.", request.Id);
            await CompleteRequestAsync(request);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CompleteRequestAsync(Request request)
    {
        request.Status = RequestStatus.Filled;
        request.ConfirmationDeadline = null;

        if (request.FilledByUserId.HasValue)
        {
            var settings = await _settingsService.GetSiteSettingsAsync();
            var fillerId = request.FilledByUserId.Value;
            var bonus = settings.FillRequestBonus + request.BountyAmount;
            
            await _userService.AddCoinsAsync(fillerId, new UpdateCoinsRequestDto { Amount = bonus });
            
            _logger.LogInformation("Awarded {Bonus} Coins to user {FillerUserId} for filling request {RequestId}.", bonus, fillerId, request.Id);

            await _notificationService.SendRequestConfirmedNotificationAsync(request);
        }
    }

    public async Task<Request?> GetRequestByIdAsync(int requestId)
    {
        return await _context.Requests
            .Include(r => r.RequestedByUser)
            .Include(r => r.FilledByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }
}

