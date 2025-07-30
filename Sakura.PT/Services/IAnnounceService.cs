using Microsoft.AspNetCore.Mvc;
using BencodeNET.Objects; // For BencodeDictionary

namespace Sakura.PT.Services;

public interface IAnnounceService
{
    Task<BDictionary> ProcessAnnounceRequest(
        string infoHash,
        string peerId,
        int port,
        long uploaded,
        long downloaded,
        long left,
        string? @event,
        int numWant,
        string? key,
        string? ipAddress,
        int userId); // Pass userId from controller
}