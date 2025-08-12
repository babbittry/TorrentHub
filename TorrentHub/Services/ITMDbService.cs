using TorrentHub.DTOs;
using System.Threading.Tasks;

namespace TorrentHub.Services;

public interface ITMDbService
{
    Task<TMDbMovieDto?> GetMovieByImdbIdAsync(string imdbId);
    Task<TMDbMovieDto?> GetMovieByTmdbIdAsync(string tmdbId, string language);
}
