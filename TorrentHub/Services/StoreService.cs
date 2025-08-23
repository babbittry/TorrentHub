using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IStoreService
{
    Task<List<StoreItemDto>> GetAvailableItemsAsync();
    Task<bool> PurchaseItemAsync(int userId, int itemId);
}

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreService> _logger;
    private readonly IDistributedCache _cache;

    // Cache key for store items
    private const string StoreItemsCacheKey = "StoreItems";
    // Cache duration (e.g., 1 hour)
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
        var cachedData = await _cache.GetStringAsync(StoreItemsCacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving store items from cache.");
            return JsonSerializer.Deserialize<List<StoreItemDto>>(cachedData) ?? new List<StoreItemDto>();
        }

        _logger.LogInformation("Cache miss for store items. Refreshing from DB.");
        var items = await _context.StoreItems
            .Where(i => i.IsAvailable)
            .Select(i => new StoreItemDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                Price = i.Price,
                IsAvailable = i.IsAvailable,
                BadgeId = i.BadgeId
            })
            .ToListAsync();
            
        await _cache.SetStringAsync(StoreItemsCacheKey, JsonSerializer.Serialize(items), _cacheOptions);
        return items;
    }

    /// <summary>
    /// Handles the purchase of a store item by a user.
    /// This method uses a database transaction to ensure atomicity:
    /// either the coins are deducted and the item is granted, or both fail.
    /// </summary>
    /// <param name="userId">The ID of the user making the purchase.</param>
    /// <param name="itemId">The ID of the item being purchased.</param>
    /// <returns>True if the purchase was successful, false otherwise.</returns>
    public async Task<bool> PurchaseItemAsync(int userId, int itemId)
    {
        // Start a database transaction to ensure data consistency.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Retrieve user and item details.
            var user = await _context.Users.FindAsync(userId);
            var item = await _context.StoreItems.FindAsync(itemId);

            // 2. Validate purchase conditions.
            // Check if user or item exists, and if the item is available for purchase.
            if (user == null || item == null || !item.IsAvailable)
            {
                _logger.LogWarning("Purchase failed: User {UserId} or item {ItemId} not found or item is unavailable.", userId, itemId);
                return false;
            }

            // Check if the user has enough Coins for the purchase.
            if (user.Coins < item.Price)
            {
                _logger.LogWarning("Purchase failed: User {UserId} has insufficient funds for item {ItemId}.", userId, itemId);
                return false;
            }

            // 3. Deduct coins from the user's balance.
            user.Coins -= item.Price;

            // 4. Grant the item's effect based on its ItemCode.
            // This switch statement handles different types of store items and applies their respective effects.
            switch (item.ItemCode)
            {
                case Enums.StoreItemCode.UploadCredit10GB:
                    user.UploadedBytes += 10L * 1024 * 1024 * 1024; // Add 10 GB upload credit
                    break;
                case Enums.StoreItemCode.UploadCredit50GB:
                    user.UploadedBytes += 50L * 1024 * 1024 * 1024; // Add 50 GB upload credit
                    break;
                case Enums.StoreItemCode.InviteOne:
                    user.InviteNum += 1; // Grant one invitation code
                    break;
                case Enums.StoreItemCode.InviteFive:
                    user.InviteNum += 5; // Grant five invitation codes
                    break;
                
                case Enums.StoreItemCode.DoubleUpload:
                    // Activate double upload status for 24 hours.
                    // If already active, this extends or resets the duration.
                    user.IsDoubleUploadActive = true;
                    user.DoubleUploadExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
                    break;
                case Enums.StoreItemCode.NoHitAndRun:
                    // Activate no Hit & Run status for 72 hours.
                    // If already active, this extends or resets the duration.
                    user.IsNoHRActive = true;
                    user.NoHRExpiresAt = DateTimeOffset.UtcNow.AddHours(72);
                    break;
                case Enums.StoreItemCode.Badge:
                    // Grant a specific badge to the user.
                    if (!item.BadgeId.HasValue)
                    {
                        _logger.LogWarning("Purchase failed: Badge item {ItemId} does not have a BadgeId associated.", itemId);
                        return false; // Badge item must have a BadgeId
                    }
                    // Prevent user from purchasing the same badge multiple times.
                    if (await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == item.BadgeId.Value))
                    {
                        _logger.LogWarning("Purchase failed: User {UserId} already owns badge {BadgeId}.", userId, item.BadgeId.Value);
                        return false;
                    }
                    _context.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = item.BadgeId.Value, AcquiredAt = DateTimeOffset.UtcNow });
                    // Invalidate user's badges cache after purchasing a new badge
                    await _cache.RemoveAsync($"UserBadges:{userId}");
                    _logger.LogInformation("User badges cache invalidated for user {UserId}.", userId);
                    break;
                default:
                    // Log an error if an unhandled item code is encountered.
                    throw new Exception($"Unhandled store item code: {item.ItemCode}");
            }

            // 5. Save changes to the database and commit the transaction.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} successfully purchased item {ItemId} ({ItemCode}).", userId, itemId, item.ItemCode);
            return true;
        }
        catch (Exception ex)
        {
            // 6. Rollback the transaction if any error occurs during the purchase process.
            _logger.LogError(ex, "An error occurred during purchase for user {UserId}, item {ItemId}. Rolling back transaction.", userId, itemId);
            await transaction.RollbackAsync();
            return false;
        }
    }
}
