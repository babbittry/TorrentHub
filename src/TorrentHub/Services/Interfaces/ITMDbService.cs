using TorrentHub.Core.DTOs;
using System.Threading.Tasks;

namespace TorrentHub.Services.Interfaces;

public interface ITMDbService
{
    Task<TMDbMovieDto?> GetMovieByImdbIdAsync(string imdbId);
    Task<TMDbMovieDto?> GetMovieByTmdbIdAsync(string tmdbId, string language);
    
    /// <summary>
    /// 通过任意输入格式获取媒体信息 (支持豆瓣ID/URL, IMDb ID/URL，电影或电视剧)
    /// </summary>
    Task<TMDbMovieDto?> GetMediaByInputAsync(string input, string language = "zh-CN");
}

