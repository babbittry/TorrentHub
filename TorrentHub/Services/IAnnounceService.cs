using Microsoft.AspNetCore.Mvc;
using BencodeNET.Objects; // For BencodeDictionary

namespace TorrentHub.Services;

public interface IAnnounceService
{
    Task<BDictionary> ProcessAnnounceRequest(
        string infoHash,
        string peerId,
        int port,
        ulong uploaded,
        ulong downloaded,
        long left,
        string? @event,
        int numWant,
        System.Net.IPAddress ipAddress,
        Guid passkey); // Pass passkey from controller
}