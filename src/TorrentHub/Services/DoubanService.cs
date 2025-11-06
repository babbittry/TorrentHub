using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TorrentHub.Core.DTOs;
using TorrentHub.Services.Configuration;

namespace TorrentHub.Services;

public class DoubanService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DoubanService> _logger;
    private readonly DoubanSettings _settings;

    public DoubanService(
        HttpClient httpClient, 
        IOptions<DoubanSettings> settings,
        ILogger<DoubanService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string?> GetImdbIdFromDoubanAsync(string doubanId)
    {
        try
        {
            var doubanUrl = $"https://movie.douban.com/subject/{doubanId}/";
            var encodedUrl = Uri.EscapeDataString(doubanUrl);
            var apiUrl = $"{_settings.PtgenBaseUrl}?url={encodedUrl}";
            
            _logger.LogInformation("Fetching IMDb ID for Douban ID: {DoubanId}", doubanId);
            
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get data from ptgen. Status: {StatusCode}", 
                    response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DoubanMediaDto>(
                content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.ImdbId))
            {
                _logger.LogInformation(
                    "Successfully got IMDb ID {ImdbId} from Douban ID {DoubanId}", 
                    result.ImdbId, doubanId);
                return result.ImdbId;
            }
            
            _logger.LogWarning("No IMDb ID found for Douban ID {DoubanId}", doubanId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching IMDb ID from Douban ID {DoubanId}", doubanId);
            return null;
        }
    }
}