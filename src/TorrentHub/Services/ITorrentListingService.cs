using TorrentHub.Core.Entities;
using TorrentHub.Core.DTOs;

namespace TorrentHub.Services;

public interface ITorrentListingService
{
    Task<PaginatedResult<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter);
}

