using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public class RequestService : IRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly SakuraCoinSettings _settings;
    private readonly ILogger<RequestService> _logger;

    public RequestService(ApplicationDbContext context, IUserService userService, IOptions<SakuraCoinSettings> settings, ILogger<RequestService> logger)
    {
        _context = context;
        _userService = userService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "User not found.", null);
        }

        if (createRequestDto.InitialBounty > 0 && user.SakuraCoins < createRequestDto.InitialBounty)
        {
            return (false, "Insufficient SakuraCoins for initial bounty.", null);
        }

        var newRequest = new Request
        {
            Title = createRequestDto.Title,
            Description = createRequestDto.Description,
            RequestedByUserId = userId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            BountyAmount = createRequestDto.InitialBounty
        };

        if (createRequestDto.InitialBounty > 0)
        {
            // Deduct initial bounty from user
            user.SakuraCoins -= createRequestDto.InitialBounty;
        }

        _context.Requests.Add(newRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created a new request titled '{RequestTitle}' with initial bounty {BountyAmount}.", userId, newRequest.Title, newRequest.BountyAmount);
        return (true, "Request created successfully.", newRequest);
    }

    public async Task<(bool Success, string Message)> AddBountyAsync(int requestId, long amount, int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "User not found.");
        }

        if (amount <= 0)
        {
            return (false, "Bounty amount must be positive.");
        }

        if (user.SakuraCoins < amount)
        {
            return (false, "Insufficient SakuraCoins to add to bounty.");
        }

        var request = await _context.Requests.FindAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending)
        {
            return (false, "Request not found or already filled/expired.");
        }

        // Deduct coins from user
        user.SakuraCoins -= amount;
        request.BountyAmount += amount;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added {Amount} SakuraCoins to request {RequestId}. New bounty: {NewBounty}.", userId, amount, requestId, request.BountyAmount);
        return (true, "Bounty added successfully.");
    }

    public async Task<List<Request>> GetRequestsAsync(RequestStatus? status)
    {
        var requests = await _context.Requests
            .Where(r => r.Status == status)
            .Include(r => r.RequestedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests;
    }

    public async Task<(bool Success, string Message)> FillRequestAsync(int requestId, int torrentId, int fillerUserId)
    {
        var request = await _context.Requests.FindAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending)
        {
            return (false, "Request not found or already filled.");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        // You can't fill your own request
        if (fillerUserId == request.RequestedByUserId)
        {
            return (false, "You cannot fill your own request.");
        }

        request.Status = RequestStatus.Filled;
        request.FilledWithTorrentId = torrentId;
        request.FilledByUserId = fillerUserId;
        request.FilledAt = DateTime.UtcNow;

        // Give the bonus to the user who uploaded the torrent
        // If there's a bounty, transfer it. Otherwise, use the default FillRequestBonus.
        if (request.BountyAmount > 0)
        {
            var transferSuccess = await _userService.TransferSakuraCoinsAsync(request.RequestedByUserId, fillerUserId, request.BountyAmount);
            if (!transferSuccess)
            {
                _logger.LogError("Failed to transfer bounty for request {RequestId} from {RequestedBy} to {Filler}.", requestId, request.RequestedByUserId, fillerUserId);
                // Decide how to handle this error: revert request status? log and proceed?
                // For now, we'll just log and proceed, but the bounty won't be transferred.
            }
            _logger.LogInformation("User {FillerUserId} filled request {RequestId} with torrent {TorrentId} and received {Bounty} SakuraCoins bounty.", fillerUserId, requestId, torrentId, request.BountyAmount);
        }
        else
        {
            await _userService.AddSakuraCoinsAsync(fillerUserId, _settings.FillRequestBonus);
            _logger.LogInformation("User {FillerUserId} filled request {RequestId} with torrent {TorrentId} and earned {Bonus} SakuraCoins.", fillerUserId, requestId, torrentId, _settings.FillRequestBonus);
        }

        await _context.SaveChangesAsync();

        return (true, "Request successfully filled.");
    }
}
