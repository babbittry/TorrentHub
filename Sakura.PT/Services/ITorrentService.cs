using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public interface ITorrentService
{
    Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, string? description, TorrentCategory category, int userId);
    Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime? freeUntil);
    Task<(bool Success, string Message)> SetStickyAsync(int torrentId, TorrentStickyStatus status);
    Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, string imdbId, int userId);
    Task<(bool Success, string Message)> ApplyFreeleechTokenAsync(int torrentId, int userId);
    Task<FileStreamResult?> DownloadTorrentAsync(int torrentId);
}
