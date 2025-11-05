namespace TorrentHub.Services.Configuration;

public class TMDbSettings
{  
    /// <summary>
    /// TMDB API Read Access Token (v4) - Preferred authentication method using Bearer token.
    /// </summary>
    public required string AccessToken { get; set; }
    
    public required string BaseUrl { get; set; }
}
