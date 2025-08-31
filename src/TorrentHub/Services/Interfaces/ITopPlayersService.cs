using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services.Interfaces;

public interface ITopPlayersService
{
    Task<List<UserPublicProfileDto>> GetTopPlayersAsync(TopPlayerType type);
    Task RefreshTopPlayersCacheAsync();
}

