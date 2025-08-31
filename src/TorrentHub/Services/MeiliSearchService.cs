using System;
using Meilisearch;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class MeiliSearchService : IMeiliSearchService
{
    private readonly MeilisearchClient _client;

    public MeiliSearchService(MeilisearchClient client)
    {
        _client = client;
    }

    public async Task IndexTorrentAsync(TorrentSearchDto torrent)
    {
        var index = _client.Index("torrents");
        await index.AddDocumentsAsync(new[] { torrent });
    }

    public async Task DeleteTorrentAsync(int torrentId)
    {
        var index = _client.Index("torrents");
        await index.DeleteOneDocumentAsync(torrentId.ToString());
    }

    public async Task IndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class
    {
        var index = _client.Index(indexName);
        await index.AddDocumentsAsync(documents);
    }

    public async Task<IEnumerable<T>> SearchAsync<T>(string indexName, string query, int page = 1, int pageSize = 20) where T : class
    {
        var index = _client.Index(indexName);
        var searchResult = await index.SearchAsync<T>(query, new SearchQuery 
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });
        return searchResult.Hits;
    }

    public async Task DeleteDocumentAsync(string indexName, string documentId)
    {
        var index = _client.Index(indexName);
        await index.DeleteOneDocumentAsync(documentId);
    }

    public async Task DeleteDocumentsAsync(string indexName, IEnumerable<string> documentIds)
    {
        var index = _client.Index(indexName);
        await index.DeleteDocumentsAsync(documentIds);
    }

    public async Task ClearIndexAsync(string indexName)
    {
        var index = _client.Index(indexName);
        await index.DeleteAllDocumentsAsync();
    }
}

