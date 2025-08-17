#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Services;

/// <summary>
/// Service class containing the business logic for managing requests (bounties).
/// </summary>
public class RequestService : IRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly CoinSettings _settings;
    private readonly ILogger<RequestService> _logger;

    public RequestService(ApplicationDbContext context, IUserService userService, IOptions<CoinSettings> settings, ILogger<RequestService> logger)
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
        if (createRequestDto.InitialBounty > 0 && user.Coins < createRequestDto.InitialBounty)
        {
            return (false, "Insufficient Coins for initial bounty.", null);
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
            user.Coins -= createRequestDto.InitialBounty;
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
    /// <param name="amount">The amount of Coins to add.</param>
    /// <param name="userId">The ID of the user adding the bounty.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string Message)> AddBountyAsync(int requestId, AddBountyRequestDto addBountyRequestDto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "User not found.");
        }

        if (addBountyRequestDto.Amount <= 0)
        {
            return (false, "Bounty amount must be positive.");
        }

        // 检查用户是否有足够的樱花币
        if (user.Coins < addBountyRequestDto.Amount)
        {
            return (false, "Insufficient Coins to add to bounty.");
        }

        var request = await _context.Requests.FindAsync(requestId);
        // 只能为“待处理”的请求添加赏金
        if (request == null || request.Status != RequestStatus.Pending)
        {
            return (false, "Request not found or already filled/expired.");
        }

        // 从用户账户扣除樱花币，并增加到请求的总赏金中
        user.Coins -= addBountyRequestDto.Amount;
        request.BountyAmount += addBountyRequestDto.Amount;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added {Amount} Coins to request {RequestId}. New bounty: {NewBounty}.", userId, addBountyRequestDto.Amount, requestId, request.BountyAmount);
        return (true, "Bounty added successfully.");
    }

    /// <summary>
    /// Retrieves a list of requests based on their status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A list of Request entities.</returns>
    public async Task<List<Request>> GetRequestsAsync(RequestStatus? status, string sortBy, string sortOrder)
    {
        var query = _context.Requests.AsQueryable();

        // 1. 如果提供了 status，则按状态进行筛选
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        // 2. 根据 sortBy 和 sortOrder 参数进行排序
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
            // 默认按创建日期降序排序
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // 3. 加载关联数据并执行查询
        var requests = await query
            .Include(r => r.RequestedByUser) // 加载请求者信息
            .Include(r => r.FilledByUser)    // 加载完成者信息
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
    public async Task<(bool Success, string Message)> FillRequestAsync(int requestId, FillRequestDto fillRequestDto, int fillerUserId)
    {
        var request = await _context.Requests.FindAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending)
        {
            return (false, "Request not found or already filled.");
        }

        var torrent = await _context.Torrents.FindAsync(fillRequestDto.TorrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        // 更新请求的状态和相关信息
        request.Status = RequestStatus.Filled;
        request.FilledWithTorrentId = fillRequestDto.TorrentId;
        request.FilledByUserId = fillerUserId;
        request.FilledAt = DateTime.UtcNow;
        
        await _userService.AddCoinsAsync(fillerUserId, new UpdateCoinsRequestDto { Amount = _settings.FillRequestBonus + request.BountyAmount });
        _logger.LogInformation("User {FillerUserId} filled request {RequestId} with torrent {TorrentId} and earned {Bonus} Coins. basic bonus: {FillRequestBonus} Bounty: {BountyAmount}", fillerUserId, requestId, fillRequestDto.TorrentId, _settings.FillRequestBonus + request.BountyAmount, _settings.FillRequestBonus, request.BountyAmount);
        
        await _context.SaveChangesAsync();

        return (true, "Request successfully filled.");
    }

    public async Task<Request?> GetRequestByIdAsync(int requestId)
    {
        return await _context.Requests
            .Include(r => r.RequestedByUser)
            .Include(r => r.FilledByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }
}
