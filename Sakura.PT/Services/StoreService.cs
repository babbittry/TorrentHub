using Sakura.PT.Data;
using Sakura.PT.Entities;
using Microsoft.EntityFrameworkCore;

namespace Sakura.PT.Services;

public interface IStoreService
{
    Task<List<StoreItem>> GetAvailableItemsAsync();
    Task<bool> PurchaseItemAsync(int userId, int itemId);
}

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreService> _logger;

    public StoreService(ApplicationDbContext context, ILogger<StoreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<StoreItem>> GetAvailableItemsAsync()
    {
        return await _context.StoreItems.Where(i => i.IsAvailable).ToListAsync();
    }

    public async Task<bool> PurchaseItemAsync(int userId, int itemId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = await _context.Users.FindAsync(userId);
            var item = await _context.StoreItems.FindAsync(itemId);

            if (user == null || item == null || !item.IsAvailable)
            {
                _logger.LogWarning("Purchase failed: User {UserId} or item {ItemId} not found or item is unavailable.", userId, itemId);
                return false; // User or item not found, or item not available
            }

            if (user.SakuraCoins < item.Price)
            {
                _logger.LogWarning("Purchase failed: User {UserId} has insufficient funds for item {ItemId}.", userId, itemId);
                return false; // Insufficient funds
            }

            // Deduct coins
            user.SakuraCoins -= item.Price;

            // Grant the item's effect
            switch (item.ItemCode)
            {
                case Enums.StoreItemCode.UploadCredit10GB:
                    user.UploadedBytes += 10L * 1024 * 1024 * 1024;
                    break;
                case Enums.StoreItemCode.UploadCredit50GB:
                    user.UploadedBytes += 50L * 1024 * 1024 * 1024;
                    break;
                case Enums.StoreItemCode.InviteOne:
                    user.InviteNum += 1;
                    break;
                case Enums.StoreItemCode.InviteFive:
                    user.InviteNum += 5;
                    break;
                
                case Enums.StoreItemCode.DoubleUpload:
                    user.IsDoubleUploadActive = true;
                    user.DoubleUploadExpiresAt = DateTime.UtcNow.AddHours(24); // 24 hours
                    break;
                case Enums.StoreItemCode.NoHitAndRun:
                    user.IsNoHRActive = true;
                    user.NoHRExpiresAt = DateTime.UtcNow.AddHours(72); // 72 hours
                    break;
                case Enums.StoreItemCode.Badge:
                    if (!item.BadgeId.HasValue)
                    {
                        _logger.LogWarning("Purchase failed: Badge item {ItemId} does not have a BadgeId associated.", itemId);
                        return false;
                    }
                    // Check if user already owns this badge
                    if (await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == item.BadgeId.Value))
                    {
                        _logger.LogWarning("Purchase failed: User {UserId} already owns badge {BadgeId}.", userId, item.BadgeId.Value);
                        return false;
                    }
                    _context.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = item.BadgeId.Value, AcquiredAt = DateTime.UtcNow });
                    break;
                default:
                    throw new Exception($"Unhandled store item code: {item.ItemCode}");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} successfully purchased item {ItemId} ('{ItemName}').", userId, itemId, item.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during purchase for user {UserId}, item {ItemId}. Rolling back transaction.", userId, itemId);
            await transaction.RollbackAsync();
            return false;
        }
    }
}
