using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface ITorrentListingService
{
    Task<List<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter);
}
