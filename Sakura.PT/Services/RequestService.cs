#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

/// <summary>
/// Service class containing the business logic for managing requests (bounties).
/// </summary>
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

    /// <summary>
    /// Creates a new request, deducting the initial bounty from the user's account if provided.
    /// </summary>
    /// <param name="createRequestDto">The DTO with request details.</param>
    /// <param name="userId">The ID of the user creating the request.</param>
    /// <returns>A tuple indicating success, a message, and the created request entity.</returns>
    public async Task<(bool Success, string Message, Request? Request)> CreateRequestAsync(CreateRequestDto createRequestDto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "User not found.", null);
        }

        // 检查用户是否有足够的樱花币来支付初始赏金
        if (createRequestDto.InitialBounty > 0 && user.SakuraCoins < createRequestDto.InitialBounty)
        {
            return (false, "Insufficient SakuraCoins for initial bounty.", null);
        }

        // 创建新请求实体
        var newRequest = new Request
        {
            Title = createRequestDto.Title,
            Description = createRequestDto.Description,
            RequestedByUserId = userId,
            Status = RequestStatus.Pending, // 初始状态为“待处理”
            CreatedAt = DateTime.UtcNow,
            BountyAmount = createRequestDto.InitialBounty
        };

        // 如果有初始赏金，则从用户账户中扣除
        if (createRequestDto.InitialBounty > 0)
        {
            user.SakuraCoins -= createRequestDto.InitialBounty;
        }

        _context.Requests.Add(newRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created a new request titled '{RequestTitle}' with initial bounty {BountyAmount}.", userId, newRequest.Title, newRequest.BountyAmount);
        return (true, "Request created successfully.", newRequest);
    }

    /// <summary>
    /// Adds more bounty to an existing, pending request.
    /// </summary>
    /// <param name="requestId">The ID of the request.</param>
    /// <param name="amount">The amount of SakuraCoins to add.</param>
    /// <param name="userId">The ID of the user adding the bounty.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string Message)> AddBountyAsync(int requestId, ulong amount, int userId)
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

        // 检查用户是否有足够的樱花币
        if (user.SakuraCoins < amount)
        {
            return (false, "Insufficient SakuraCoins to add to bounty.");
        }

        var request = await _context.Requests.FindAsync(requestId);
        // 只能为“待处理”的请求添加赏金
        if (request == null || request.Status != RequestStatus.Pending)
        {
            return (false, "Request not found or already filled/expired.");
        }

        // 从用户账户扣除樱花币，并增加到请求的总赏金中
        user.SakuraCoins -= amount;
        request.BountyAmount += amount;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added {Amount} SakuraCoins to request {RequestId}. New bounty: {NewBounty}.", userId, amount, requestId, request.BountyAmount);
        return (true, "Bounty added successfully.");
    }

    /// <summary>
    /// Retrieves a list of requests based on their status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A list of Request entities.</returns>
    public async Task<List<Request>> GetRequestsAsync(RequestStatus? status)
    {
        var requests = await _context.Requests
            .Where(r => r.Status == status) // 根据状态过滤
            .Include(r => r.RequestedByUser) // 加载关联的请求者用户信息
            .OrderByDescending(r => r.CreatedAt) // 按创建时间降序排序
            .ToListAsync();

        return requests;
    }

    /// <summary>
    /// Marks a request as 'Filled' by linking it to a torrent.
    /// Transfers the bounty to the user who filled the request.
    /// </summary>
    /// <param name="requestId">The ID of the request being filled.</param>
    /// <param name="torrentId">The ID of the torrent fulfilling the request.</param>
    /// <param name="fillerUserId">The ID of the user filling the request.</param>
    /// <returns>A tuple indicating success and a message.</returns>
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

        // 更新请求的状态和相关信息
        request.Status = RequestStatus.Filled;
        request.FilledWithTorrentId = torrentId;
        request.FilledByUserId = fillerUserId;
        request.FilledAt = DateTime.UtcNow;
        
        await _userService.AddSakuraCoinsAsync(fillerUserId, _settings.FillRequestBonus + request.BountyAmount);
        _logger.LogInformation("User {FillerUserId} filled request {RequestId} with torrent {TorrentId} and earned {Bonus} SakuraCoins. basic bonus: {FillRequestBonus} Bounty: {BountyAmount}", fillerUserId, requestId, torrentId, _settings.FillRequestBonus + request.BountyAmount, _settings.FillRequestBonus, request.BountyAmount);
        
        await _context.SaveChangesAsync();

        return (true, "Request successfully filled.");
    }
}
