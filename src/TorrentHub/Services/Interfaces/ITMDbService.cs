using TorrentHub.Core.DTOs;
using System.Threading.Tasks;

namespace TorrentHub.Services.Interfaces;

public interface ITMDbService
{
    Task<TMDbMovieDto?> GetMovieByImdbIdAsync(string imdbId);
    Task<TMDbMovieDto?> GetMovieByTmdbIdAsync(string tmdbId, string language);
}

