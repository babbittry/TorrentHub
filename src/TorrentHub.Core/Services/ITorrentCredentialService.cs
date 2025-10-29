using TorrentHub.Core.Entities;

namespace TorrentHub.Core.Services;

public interface ITorrentCredentialService
{
    /// <summary>
    /// 获取或创建用户对指定种子的credential
    /// 采用Get-or-Create策略:如果已存在未撤销的credential则复用,否则创建新的
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="torrentId">种子ID</param>
    /// <returns>Credential GUID</returns>
    Task<Guid> GetOrCreateCredentialAsync(int userId, int torrentId);

    /// <summary>
    /// 验证credential并返回关联的用户ID和种子ID
    /// </summary>
    /// <param name="credential">Credential GUID</param>
    /// <returns>如果有效返回(IsValid=true, UserId, TorrentId),否则返回(IsValid=false, null, null)</returns>
    Task<(bool IsValid, int? UserId, int? TorrentId)> ValidateCredentialAsync(Guid credential);

    /// <summary>
    /// 撤销指定的credential
    /// </summary>
    /// <param name="credential">要撤销的credential GUID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>撤销是否成功</returns>
    Task<bool> RevokeCredentialAsync(Guid credential, string reason);

    /// <summary>
    /// 撤销用户对指定种子的所有credentials
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="torrentId">种子ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>撤销的credential数量</returns>
    /// <summary>
    /// 撤销用户的所有credentials
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>撤销的credential数量</returns>
    Task<int> RevokeUserCredentialsAsync(int userId, string reason);

    Task<int> RevokeUserTorrentCredentialsAsync(int userId, int torrentId, string reason);

    /// <summary>
    /// 更新credential的使用记录
    /// </summary>
    /// <param name="credential">Credential GUID</param>
    Task UpdateCredentialUsageAsync(Guid credential);

    /// <summary>
    /// 获取用户的所有credentials
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="includeRevoked">是否包含已撤销的credentials</param>
    /// <returns>Credential列表</returns>
    Task<List<TorrentCredential>> GetUserCredentialsAsync(int userId, bool includeRevoked = false);

    /// <summary>
    /// 清理长期未使用的inactive credentials
    /// </summary>
    /// <param name="inactiveDays">多少天未使用视为inactive</param>
    /// <returns>清理的credential数量</returns>
    Task<int> CleanupInactiveCredentialsAsync(int inactiveDays = 90);
}