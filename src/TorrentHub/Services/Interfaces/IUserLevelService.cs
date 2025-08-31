namespace TorrentHub.Services;

public interface IUserLevelService
{
    Task CheckAndPromoteDemoteUsersAsync();
}
