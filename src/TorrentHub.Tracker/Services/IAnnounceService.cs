using Microsoft.AspNetCore.Http;

namespace TorrentHub.Tracker.Services;

public interface IAnnounceService
{
    /// <summary>
    /// Process announce request using credential-based authentication
    /// </summary>
    Task<byte[]> HandleAnnounceAsync(HttpContext context);
}
