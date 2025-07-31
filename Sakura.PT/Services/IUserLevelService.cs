namespace Sakura.PT.Services;

public interface IUserLevelService
{
    Task CheckAndPromoteDemoteUsersAsync();
}
