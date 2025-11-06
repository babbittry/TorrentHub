using System.Text.Json.Serialization;

namespace TorrentHub.Core.DTOs;

public class DoubanMediaDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }
    
    [JsonPropertyName("imdb_link")]
    public string? ImdbLink { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}