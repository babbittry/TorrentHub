using System.Text.Json.Serialization;

namespace TorrentHub.Core.DTOs;

public class TMDbMovieDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("genres")]
    public required Genre[] Genres { get; set; }

    [JsonPropertyName("credits")]
    public required Credits Credits { get; set; }

    [JsonPropertyName("vote_average")]
    public double? VoteAverage { get; set; }
}

public class Genre
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class Credits
{
    [JsonPropertyName("cast")]
    public required CastMember[] Cast { get; set; }

    [JsonPropertyName("crew")]
    public required CrewMember[] Crew { get; set; }
}

public class CastMember
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public class CrewMember
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("job")]
    public required string Job { get; set; }
}

// Helper DTOs for intermediate API calls

public class TMDbFindResult
{
    [JsonPropertyName("movie_results")]
    public required TMDbMovieStub[] MovieResults { get; set; }
}

public class TMDbMovieStub
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
