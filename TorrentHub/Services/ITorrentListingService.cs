using TorrentHub.Entities;
using TorrentHub.DTOs;

namespace TorrentHub.Services;

public interface ITorrentListingService
{
    Task<List<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter);
}
