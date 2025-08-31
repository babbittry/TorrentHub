using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using System.Text.Json;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services;

public class PurchaseResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IStoreService
{
    Task<List<StoreItemDto>> GetAvailableItemsAsync();
    Task<PurchaseResultDto> PurchaseItemAsync(int userId, PurchaseItemRequestDto request);
}

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreService> _logger;
    private readonly IDistributedCache _cache;

    private const string StoreItemsCacheKey = "StoreItems";
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public StoreService(ApplicationDbContext context, ILogger<StoreService> logger, IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<StoreItemDto>> GetAvailableItemsAsync()
    {
        var languageCode = CultureInfo.CurrentUICulture.Name.Split('-')[0];
        var cacheKey = $"{StoreItemsCacheKey}:{languageCode}";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving store items from cache for language {Language}.", languageCode);
            return JsonSerializer.Deserialize<List<StoreItemDto>>(cachedData) ?? new List<StoreItemDto>();
        }

        _logger.LogInformation("Cache miss for store items (Language: {Language}). Refreshing from DB.", languageCode);
        var items = await _context.StoreItems
            .Where(i => i.IsAvailable)
            .Include(i => i.Translations)
            .Select(i => new StoreItemDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                Name = i.Translations.Any(t => t.Language == languageCode)
                    ? i.Translations.First(t => t.Language == languageCode).Name
                    : i.Name,
                Description = i.Translations.Any(t => t.Language == languageCode)
                    ? i.Translations.First(t => t.Language == languageCode).Description
                    : i.Description,
                Price = i.Price,
                IsAvailable = i.IsAvailable,
                BadgeId = i.BadgeId
            })
            .ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(items), _cacheOptions);
        return items;
    }

    public async Task<PurchaseResultDto> PurchaseItemAsync(int userId, PurchaseItemRequestDto request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = await _context.Users.FindAsync(userId);
            var item = await _context.StoreItems.FindAsync(request.StoreItemId);

            if (user == null || item == null || !item.IsAvailable)
            {
                return new PurchaseResultDto { Success = false, Message = "Item not found or is unavailable." };
            }

            var totalCost = item.Price * (ulong)request.Quantity;
            if (user.Coins < totalCost)
            {
                return new PurchaseResultDto { Success = false, Message = "Insufficient Sakura Coins." };
            }

            // Specific validation for badge items
            if (item.ItemCode == StoreItemCode.Badge)
            {
                if (request.Quantity > 1)
                {
                    return new PurchaseResultDto { Success = false, Message = "Badges can only be purchased one at a time." };
                }
                if (!item.BadgeId.HasValue)
                {
                    _logger.LogWarning("Purchase failed: Badge item {ItemId} does not have a BadgeId associated.", item.Id);
                    return new PurchaseResultDto { Success = false, Message = "Invalid badge item configuration." };
                }
                if (await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == item.BadgeId.Value))
                {
                    return new PurchaseResultDto { Success = false, Message = "You already own this badge." };
                }
            }

            user.Coins -= totalCost;

            switch (item.ItemCode)
            {
                case StoreItemCode.UploadCredit10GB:
                    user.UploadedBytes += (ulong)request.Quantity * 10UL * 1024 * 1024 * 1024;
                    break;
                case StoreItemCode.UploadCredit50GB:
                    user.UploadedBytes += (ulong)request.Quantity * 50UL * 1024 * 1024 * 1024;
                    break;
                case StoreItemCode.InviteOne:
                    user.InviteNum += (uint)request.Quantity;
                    break;
                case StoreItemCode.InviteFive:
                    user.InviteNum += (uint)(request.Quantity * 5);
                    break;
                
                // Items that likely shouldn't be bought in quantity - we can restrict if needed, but for now let's assume quantity=1
                case StoreItemCode.DoubleUpload:
                    user.IsDoubleUploadActive = true;
                    if (user.DoubleUploadExpiresAt.HasValue && user.DoubleUploadExpiresAt.Value > DateTimeOffset.UtcNow)
                    {
                        user.DoubleUploadExpiresAt = (DateTimeOffset?)user.DoubleUploadExpiresAt.Value.AddHours(24 * request.Quantity);
                    }
                    else
                    {
                        user.DoubleUploadExpiresAt = DateTimeOffset.UtcNow.AddHours(24 * request.Quantity);
                    }
                    break;
                case StoreItemCode.NoHitAndRun:
                    user.IsNoHRActive = true;
                    if (user.NoHRExpiresAt.HasValue && user.NoHRExpiresAt.Value > DateTimeOffset.UtcNow)
                    {
                        user.NoHRExpiresAt = (DateTimeOffset?)user.NoHRExpiresAt.Value.AddHours(72 * request.Quantity);
                    }
                    else
                    {
                        user.NoHRExpiresAt = DateTimeOffset.UtcNow.AddHours(72 * request.Quantity);
                    }
                    break;
                case StoreItemCode.Badge:
                    // Logic is already validated above for quantity=1 and uniqueness
                    _context.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = item.BadgeId!.Value, AcquiredAt = DateTimeOffset.UtcNow });
                    await _cache.RemoveAsync($"UserBadges:{userId}");
                    _logger.LogInformation("User badges cache invalidated for user {UserId}.", userId);
                    break;
                case StoreItemCode.ChangeUsername:
                    if (request.Params == null || !request.Params.TryGetValue("newUsername", out var newUsernameObj) || newUsernameObj is not string newUsername || string.IsNullOrWhiteSpace(newUsername))
                    {
                        return new PurchaseResultDto { Success = false, Message = "New username must be provided for this item." };
                    }
                    if (await _context.Users.AnyAsync(u => u.UserName == newUsername))
                    {
                        return new PurchaseResultDto { Success = false, Message = "This username is already taken." };
                    }
                    user.UserName = newUsername;
                    _logger.LogInformation("User {UserId} changed their username to {NewUsername}.", userId, newUsername);
                    break;
                default:
                    throw new Exception($"Unhandled store item code: {item.ItemCode}");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} successfully purchased item {ItemId} (Code: {ItemCode}, Quantity: {Quantity}).", userId, item.Id, item.ItemCode, request.Quantity);
            return new PurchaseResultDto { Success = true, Message = "Purchase successful!" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during purchase for user {UserId}, item {ItemId}. Rolling back transaction.", userId, request.StoreItemId);
            await transaction.RollbackAsync();
            // In a real-world scenario, you might not want to expose raw exception messages.
            return new PurchaseResultDto { Success = false, Message = "An unexpected error occurred. The transaction has been rolled back." };
        }
    }
}

