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
    private readonly IUserService _userService;

    private const string StoreItemsCacheKey = "StoreItems";
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public StoreService(ApplicationDbContext context, ILogger<StoreService> logger, IDistributedCache cache, IUserService userService)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _userService = userService;
    }

    private static string? ExtractStringParameter(Dictionary<string, object>? parameters, string key)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            string str => str,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => value?.ToString()
        };
    }

    public async Task<List<StoreItemDto>> GetAvailableItemsAsync()
    {
        var cachedData = await _cache.GetStringAsync(StoreItemsCacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving store items from cache.");
            return JsonSerializer.Deserialize<List<StoreItemDto>>(cachedData) ?? new List<StoreItemDto>();
        }

        _logger.LogInformation("Cache miss for store items. Refreshing from DB.");
        
        var itemsFromDb = await _context.StoreItems
            .Where(i => i.IsAvailable)
            .ToListAsync();

        var items = itemsFromDb.Select(item =>
        {
            var (actionType, metadata) = MapItemToAction(item);

            return new StoreItemDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                NameKey = $"store.items.{item.ItemCode.ToString().ToLowerInvariant()}.name",
                DescriptionKey = $"store.items.{item.ItemCode.ToString().ToLowerInvariant()}.description",
                Price = item.Price,
                IsAvailable = item.IsAvailable,
                ActionType = actionType,
                ActionMetadata = metadata
            };
        }).ToList();
        
        await _cache.SetStringAsync(StoreItemsCacheKey, JsonSerializer.Serialize(items), _cacheOptions);
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
                return new PurchaseResultDto { Success = false, Message = "Insufficient Coins." };
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
                case StoreItemCode.UploadCredit100GB:
                    user.UploadedBytes += (ulong)request.Quantity * 100UL * 1024 * 1024 * 1024;
                    break;
                case StoreItemCode.InviteOne:
                    for (var i = 0; i < request.Quantity; i++)
                    {
                        await _userService.GenerateInviteAsync(userId, chargeForInvite: false);
                    }
                    break;
                case StoreItemCode.InviteFive:
                    // Each item purchase gives 5 invites
                    var invitesToGenerate = request.Quantity * 5;
                    for (var i = 0; i < invitesToGenerate; i++)
                    {
                        await _userService.GenerateInviteAsync(userId, chargeForInvite: false);
                    }
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
                    var newUsername = ExtractStringParameter(request.Params, "newUsername");

                    if (string.IsNullOrWhiteSpace(newUsername))
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
                case StoreItemCode.UserTitle:
                    var newTitle = ExtractStringParameter(request.Params, "newTitle");

                    if (string.IsNullOrWhiteSpace(newTitle))
                    {
                        return new PurchaseResultDto { Success = false, Message = "New title must be provided for this item." };
                    }

                    if (newTitle.Length > 30) // Fallback validation
                    {
                        return new PurchaseResultDto { Success = false, Message = "Title is too long." };
                    }

                    user.UserTitle = newTitle;
                    _logger.LogInformation("User {UserId} set their title.", userId);
                    break;
                case StoreItemCode.ColorfulUsername:
                    var duration = TimeSpan.FromDays(7 * request.Quantity);
                    if (user.ColorfulUsernameExpiresAt.HasValue && user.ColorfulUsernameExpiresAt.Value > DateTimeOffset.UtcNow)
                    {
                        user.ColorfulUsernameExpiresAt = user.ColorfulUsernameExpiresAt.Value.Add(duration);
                    }
                    else
                    {
                        user.ColorfulUsernameExpiresAt = DateTimeOffset.UtcNow.Add(duration);
                    }
                    _logger.LogInformation("User {UserId} purchased colorful username for {Days} days.", userId, duration.TotalDays);
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
    private (StoreActionType, ActionMetadata?) MapItemToAction(StoreItem item)
    {
        switch (item.ItemCode)
        {
            case StoreItemCode.UploadCredit10GB:
            case StoreItemCode.UploadCredit100GB:
            case StoreItemCode.InviteOne:
            case StoreItemCode.InviteFive:
                return (StoreActionType.PurchaseWithQuantity, new ActionMetadata
                {
                    Min = 1,
                    Max = 100,
                    Step = 1,
                    UnitKey = "unit.item"
                });

            case StoreItemCode.DoubleUpload:
            case StoreItemCode.NoHitAndRun:
                return (StoreActionType.PurchaseWithQuantity, new ActionMetadata
                {
                    Min = 1,
                    Max = 5,
                    Step = 1,
                    UnitKey = "unit.day"
                });

            case StoreItemCode.ChangeUsername:
                return (StoreActionType.ChangeUsername, new ActionMetadata
                {
                    InputLabelKey = "store.metadata.newUsername.label",
                    PlaceholderKey = "store.metadata.newUsername.placeholder"
                });

            case StoreItemCode.Badge:
                return (StoreActionType.PurchaseBadge, new ActionMetadata
                {
                    BadgeId = item.BadgeId
                });

            default:
                return (StoreActionType.SimplePurchase, null);

            case StoreItemCode.UserTitle:
                return (StoreActionType.ChangeUsername, new ActionMetadata // Re-use the same action type
                {
                    InputLabelKey = "store.metadata.newTitle.label",
                    PlaceholderKey = "store.metadata.newTitle.placeholder",
                    MaxLength = item.MaxStringLength
                });
            
            case StoreItemCode.ColorfulUsername:
                 return (StoreActionType.PurchaseWithQuantity, new ActionMetadata
                {
                    Min = 1,
                    Max = 10,
                    Step = 1,
                    UnitKey = "unit.week" // Assuming 1 quantity = 1 week
                });

        }
    }
}

