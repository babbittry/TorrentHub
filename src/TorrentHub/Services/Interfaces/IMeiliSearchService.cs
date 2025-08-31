using System.Collections.Generic;
using System.Threading.Tasks;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services.Interfaces
{
    public interface IMeiliSearchService
    {
        Task IndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class;
        Task<IEnumerable<T>> SearchAsync<T>(string indexName, string query, int page = 1, int pageSize = 20) where T : class;
        Task DeleteDocumentAsync(string indexName, string documentId);
        Task DeleteDocumentsAsync(string indexName, IEnumerable<string> documentIds);
        Task ClearIndexAsync(string indexName);
        Task IndexTorrentAsync(TorrentSearchDto torrent);
        Task DeleteTorrentAsync(int torrentId);
    }
}
