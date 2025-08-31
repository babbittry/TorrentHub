using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.Enums;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services.Interfaces;

public interface ITorrentService
{
    Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, UploadTorrentRequestDto request, int userId);
    Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime freeUntil);
    Task<(bool Success, string Message)> SetStickyAsync(int torrentId, SetStickyRequestDto request);
    Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, CompleteInfoRequestDto request, int userId);
    Task<(bool Success, string Message)> ApplyFreeleechAsync(int torrentId, int userId);
    Task<FileStreamResult?> DownloadTorrentAsync(int torrentId, int userId);
    Task<Torrent?> GetTorrentByIdAsync(int torrentId);
    Task<(bool Success, string Message)> DeleteTorrentAsync(int torrentId, int userId);
}
