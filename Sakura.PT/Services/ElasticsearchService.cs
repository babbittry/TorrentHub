using Nest;
using Sakura.PT.Entities;

namespace Sakura.PT.Services
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(IElasticClient elasticClient, ILogger<ElasticsearchService> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task IndexTorrentAsync(Torrent torrent)
        {
            var response = await _elasticClient.IndexDocumentAsync(torrent);
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index torrent {TorrentId}: {DebugInfo}", torrent.Id, response.DebugInformation);
            }
        }

        public async Task DeleteTorrentAsync(int torrentId)
        {
            var response = await _elasticClient.DeleteAsync<Torrent>(torrentId);
            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete torrent {TorrentId}: {DebugInfo}", torrentId, response.DebugInformation);
            }
        }

        public async Task<IEnumerable<Torrent>> SearchTorrentsAsync(string searchTerm, int page, int pageSize)
        {
            var response = await _elasticClient.SearchAsync<Torrent>(s => s
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(searchTerm)
                        .Fields(f => f.Field(ff => ff.Name, 2).Field(ff => ff.Description)) // Boost name field
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .From((page - 1) * pageSize)
                .Size(pageSize)
            );

            if (!response.IsValid)
            {
                _logger.LogError("Failed to search torrents: {DebugInfo}", response.DebugInformation);
                return Enumerable.Empty<Torrent>();
            }

            return response.Documents;
        }
    }
}
