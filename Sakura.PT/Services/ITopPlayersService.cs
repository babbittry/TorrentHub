using Sakura.PT.DTOs;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public interface ITopPlayersService
{
    Task<List<UserPublicProfileDto>> GetTopPlayersAsync(TopPlayerType type);
    Task RefreshTopPlayersCacheAsync();
}
