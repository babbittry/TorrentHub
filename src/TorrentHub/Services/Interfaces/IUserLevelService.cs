namespace TorrentHub.Services;

using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

public interface IUserLevelService
{
    Task CheckAndPromoteDemoteUsersAsync();
    UserLevelDto GetUserLevel(User user);
}
