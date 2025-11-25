using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Services.Configuration;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class TMDbService : ITMDbService
{
    private readonly HttpClient _httpClient;
    private readonly TMDbSettings _tmDbSettings;
    private readonly ILogger<TMDbService> _logger;
    private readonly MediaInputParser _inputParser;
    private readonly DoubanService _doubanService;

    public TMDbService(
        HttpClient httpClient,
        IOptions<TMDbSettings> tmDbSettings,
        MediaInputParser inputParser,
        DoubanService doubanService,
        ILogger<TMDbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tmDbSettings = tmDbSettings.Value;
        _inputParser = inputParser;
        _doubanService = doubanService;
    }

    public async Task<TMDbMovieDto?> GetMovieByImdbIdAsync(string imdbId, string language = "zh-CN")
    {
        if (string.IsNullOrWhiteSpace(imdbId))
        {
            return null;
        }

        try
        {
            // Step 1: Find the TMDb ID from the IMDb ID
            var findUrl = $"find/{imdbId}?external_source=imdb_id";
            var findResponse = await _httpClient.GetAsync(findUrl);

            if (!findResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to find TMDb ID for IMDb ID {ImdbId}. Status: {StatusCode}", imdbId, findResponse.StatusCode);
                return null;
            }

            var findContent = await findResponse.Content.ReadAsStringAsync();
            var findResult = JsonSerializer.Deserialize<TMDbFindResult>(findContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var movieResult = findResult?.MovieResults.FirstOrDefault();
            if (movieResult == null)
            {
                _logger.LogWarning("No movie results found for IMDb ID {ImdbId}", imdbId);
                return null;
            }

            var tmdbId = movieResult.Id;

            // Step 2: Get the full movie details using the TMDb ID
            var movieUrl = $"movie/{tmdbId}?append_to_response=credits,images&language={language}";
            var movieResponse = await _httpClient.GetAsync(movieUrl);

            if (!movieResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get movie details for TMDb ID {TMDbId}. Status: {StatusCode}", tmdbId, movieResponse.StatusCode);
                return null;
            }

            var movieContent = await movieResponse.Content.ReadAsStringAsync();
            var movieDto = JsonSerializer.Deserialize<TMDbMovieDto>(movieContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return movieDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching movie data for IMDb ID {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<TMDbMovieDto?> GetMovieByTmdbIdAsync(string tmdbId, string language)
    {
        if (string.IsNullOrWhiteSpace(tmdbId))
        {
            return null;
        }

        try
        {
            var movieUrl = $"movie/{tmdbId}?language={language}&append_to_response=credits,images";
            var movieResponse = await _httpClient.GetAsync(movieUrl);

            if (!movieResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get movie details for TMDb ID {TMDbId}. Status: {StatusCode}", tmdbId, movieResponse.StatusCode);
                return null;
            }

            var movieContent = await movieResponse.Content.ReadAsStringAsync();
            var movieDto = JsonSerializer.Deserialize<TMDbMovieDto>(movieContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Manually extract ImdbId from the main movie details endpoint if it's not in the standard DTO mapping
            using (var jsonDoc = JsonDocument.Parse(movieContent))
            {
                if (movieDto != null && jsonDoc.RootElement.TryGetProperty("imdb_id", out var imdbIdElement))
                {
                    movieDto.ImdbId = imdbIdElement.GetString();
                }
            }

            return movieDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching movie data for TMDb ID {TMDbId}", tmdbId);
            return null;
        }
    }

    public async Task<TMDbMovieDto?> GetMediaByInputAsync(string input, string language = "zh-CN")
    {
        // 1. 解析输入
        var parsedInput = _inputParser.Parse(input);
        
        if (!parsedInput.IsValid)
        {
            _logger.LogWarning("Invalid input: {Input}, Error: {Error}",
                input, parsedInput.ErrorMessage);
            return null;
        }
        
        string? imdbId = null;
        
        // 2. 根据类型获取IMDb ID
        switch (parsedInput.Source)
        {
            case MediaIdSource.DoubanId:
            case MediaIdSource.DoubanUrl:
                // 从豆瓣服务获取IMDb ID
                imdbId = await _doubanService.GetImdbIdFromDoubanAsync(parsedInput.Id!);
                break;
                
            case MediaIdSource.ImdbId:
            case MediaIdSource.ImdbUrl:
                // 直接使用IMDb ID
                imdbId = parsedInput.Id;
                break;
        }
        
        if (string.IsNullOrEmpty(imdbId))
        {
            _logger.LogWarning("Could not obtain IMDb ID from input: {Input}", input);
            return null;
        }
        
        // 3. 从TMDB获取媒体数据
        return await GetMovieByImdbIdAsync(imdbId, language);
    }
}
