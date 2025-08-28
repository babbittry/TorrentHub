using TorrentHub.Entities;
using TorrentHub.DTOs;

namespace TorrentHub.Services;

public interface ITorrentListingService
{
    Task<PaginatedResult<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter);
}
