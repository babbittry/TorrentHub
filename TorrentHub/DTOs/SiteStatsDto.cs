namespace TorrentHub.DTOs;

public class SiteStatsDto
{
    // User stats
    /// <summary>
    /// Total number of active (not banned) users.
    /// </summary>
    public long TotalUsers { get; set; }
    public long TotalBannedUsers { get; set; }
    public long UsersRegisteredToday { get; set; }
    public Dictionary<string, long> UserRoleCounts { get; set; } = new();
    // Torrent stats
    public long TotalTorrents { get; set; }
    public long TorrentsAddedToday { get; set; }
    public long DeadTorrents { get; set; }
    public ulong TotalTorrentsSize { get; set; }
    public long TotalPeers { get; set; }
    public long TotalSeeders { get; set; }
    public long TotalLeechers { get; set; }
    public ulong TotalUploaded { get; set; }
    public ulong TotalDownloaded { get; set; }
    public ulong NominalUploaded { get; set; }
    public ulong NominalDownloaded { get; set; }
    // Request stats
    public long TotalRequests { get; set; }
    public long FilledRequests { get; set; }

    // Forum stats
    public long TotalForumTopics { get; set; }
    public long TotalForumPosts { get; set; }
}