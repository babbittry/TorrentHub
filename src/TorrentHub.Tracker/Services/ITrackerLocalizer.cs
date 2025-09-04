using BencodeNET.Objects;

namespace TorrentHub.Tracker.Services;

public interface ITrackerLocalizer
{
    BDictionary GetError(string key, string? language);
}