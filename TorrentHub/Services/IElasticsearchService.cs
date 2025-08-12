using TorrentHub.Entities;

namespace TorrentHub.Services
{
    public interface IElasticsearchService
    {
        Task IndexTorrentAsync(Torrent torrent);
        Task DeleteTorrentAsync(int torrentId);
        Task<IEnumerable<Torrent>> SearchTorrentsAsync(string searchTerm, int page, int pageSize);
    }
}
