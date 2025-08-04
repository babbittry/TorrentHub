namespace Sakura.PT.Services
{
    public class TorrentSettings
    {
        public required string TorrentStoragePath { get; init; } = "./torrents";
        public int AnnounceIntervalSeconds {get; init;} = 1800;
        public int MinAnnounceIntervalSeconds {get; init;} = 900;
    }
}
