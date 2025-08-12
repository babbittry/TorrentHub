using Elastic.Clients.Elasticsearch;
using TorrentHub.Entities;

namespace TorrentHub.Services
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(ElasticsearchClient elasticClient, ILogger<ElasticsearchService> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task IndexTorrentAsync(Torrent torrent)
        {
            var response = await _elasticClient.IndexAsync(torrent, idx => idx.Index("torrents"));
            if (!response.IsSuccess())
            {
                _logger.LogError("Failed to index torrent {TorrentId}: {DebugInfo}", torrent.Id, response.DebugInformation);
            }
        }

        public async Task DeleteTorrentAsync(int torrentId)
        {
            var response = await _elasticClient.DeleteAsync("torrents", torrentId);
            if (!response.IsSuccess())
            {
                _logger.LogError("Failed to delete torrent {TorrentId}: {DebugInfo}", torrentId, response.DebugInformation);
            }
        }

        public async Task<IEnumerable<Torrent>> SearchTorrentsAsync(string searchTerm, int page, int pageSize)
        {
            var response = await _elasticClient.SearchAsync<Torrent>(s => s
                .Indices("torrents")
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(searchTerm)
                        .Fields("name^2,description")
                        .Fuzziness(new Fuzziness("AUTO"))
                    )
                )
                .From((page - 1) * pageSize)
                .Size(pageSize)
            );

            if (!response.IsSuccess())
            {
                _logger.LogError("Failed to search torrents: {DebugInfo}", response.DebugInformation);
                return Enumerable.Empty<Torrent>();
            }

            return response.Documents;
        }
    }
}