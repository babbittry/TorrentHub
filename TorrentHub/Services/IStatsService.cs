namespace TorrentHub.Services;

public interface IStatsService
{
    Task<object> GetSiteStatsAsync();
}
