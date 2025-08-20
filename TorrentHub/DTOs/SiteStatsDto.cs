namespace TorrentHub.DTOs;

public class SiteStatsDto
{
    public long TotalUsers { get; set; }
    public Dictionary<string, long> UserRoleCounts { get; set; } = new();
    public long TotalTorrents { get; set; }
    public long DeadTorrents { get; set; }
    public ulong TotalTorrentsSize { get; set; }
    public long TotalPeers { get; set; }
    public ulong TotalUploaded { get; set; }
    public ulong TotalDownloaded { get; set; }
    public ulong DisplayTotalUploaded { get; set; }
    public ulong DisplayTotalDownloaded { get; set; }
}