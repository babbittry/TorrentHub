using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;

namespace TorrentHub.Services
{
    public class RssFeedTokenService : IRssFeedTokenService
    {
        private readonly ApplicationDbContext _dbContext;

        public RssFeedTokenService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RssFeedToken> CreateTokenAsync(int userId, string feedType, string? name = null, string[]? categoryFilter = null, int maxResults = 50, DateTimeOffset? expiresAt = null)
        {
            // 获取User实体以满足required属性
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found", nameof(userId));
            }

            var token = new RssFeedToken
            {
                UserId = userId,
                User = user,
                Token = Guid.NewGuid(),
                FeedType = feedType,
                Name = name,
                CategoryFilter = categoryFilter,
                MaxResults = maxResults,
                ExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UsageCount = 0
            };

            _dbContext.RssFeedTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return token;
        }

        public async Task<RssFeedToken?> GetTokenAsync(Guid token)
        {
            return await _dbContext.RssFeedTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);
        }

        public async Task<List<RssFeedToken>> GetUserTokensAsync(int userId)
        {
            return await _dbContext.RssFeedTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateTokenUsageAsync(int tokenId, string ipAddress, string userAgent)
        {
            var token = await _dbContext.RssFeedTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.LastUsedAt = DateTimeOffset.UtcNow;
                token.LastIp = ipAddress;
                token.UserAgent = userAgent;
                token.UsageCount++;

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> RevokeTokenAsync(int tokenId, int userId)
        {
            var token = await _dbContext.RssFeedTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId);

            if (token == null || !token.IsActive)
            {
                return false;
            }

            token.IsActive = false;
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await _dbContext.RssFeedTokens
                .Where(t => t.UserId == userId && t.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsActive = false;
                token.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task CleanupExpiredTokensAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var expiredTokens = await _dbContext.RssFeedTokens
                .Where(t => t.IsActive && t.ExpiresAt.HasValue && t.ExpiresAt.Value < now)
                .ToListAsync();

            foreach (var token in expiredTokens)
            {
                token.IsActive = false;
                token.RevokedAt = now;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ValidateTokenAsync(Guid token)
        {
            var rssFeedToken = await _dbContext.RssFeedTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

            if (rssFeedToken == null)
            {
                return false;
            }

            // 检查是否过期
            if (rssFeedToken.ExpiresAt.HasValue && rssFeedToken.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                // 自动撤销过期的token
                rssFeedToken.IsActive = false;
                rssFeedToken.RevokedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync();
                return false;
            }

            return true;
        }
    }
}