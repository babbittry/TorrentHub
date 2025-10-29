using TorrentHub.Core.Entities;

namespace TorrentHub.Core.Services
{
    public interface IRssFeedTokenService
    {
        /// <summary>
        /// 创建新的RSS Feed Token
        /// </summary>
        Task<RssFeedToken> CreateTokenAsync(int userId, string feedType, string? name = null, string[]? categoryFilter = null, int maxResults = 50, DateTimeOffset? expiresAt = null);
        
        /// <summary>
        /// 通过Token获取RSS Feed Token信息
        /// </summary>
        Task<RssFeedToken?> GetTokenAsync(Guid token);
        
        /// <summary>
        /// 获取用户的所有RSS Feed Tokens
        /// </summary>
        Task<List<RssFeedToken>> GetUserTokensAsync(int userId);
        
        /// <summary>
        /// 更新Token的最后使用时间和使用次数
        /// </summary>
        Task UpdateTokenUsageAsync(int tokenId, string ipAddress, string userAgent);
        
        /// <summary>
        /// 撤销指定Token
        /// </summary>
        Task<bool> RevokeTokenAsync(int tokenId, int userId);
        
        /// <summary>
        /// 撤销用户的所有Token
        /// </summary>
        Task RevokeAllUserTokensAsync(int userId);
        
        /// <summary>
        /// 清理过期的Token
        /// </summary>
        Task CleanupExpiredTokensAsync();
        
        /// <summary>
        /// 验证Token是否有效且未过期
        /// </summary>
        Task<bool> ValidateTokenAsync(Guid token);
    }
}