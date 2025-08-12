using TorrentHub.DTOs;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public interface ITopPlayersService
{
    Task<List<UserPublicProfileDto>> GetTopPlayersAsync(TopPlayerType type);
    Task RefreshTopPlayersCacheAsync();
}
