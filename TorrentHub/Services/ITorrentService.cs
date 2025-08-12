using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Enums;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface ITorrentService
{
    Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, UploadTorrentRequestDto request, int userId);
    Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime freeUntil);
    Task<(bool Success, string Message)> SetStickyAsync(int torrentId, SetStickyRequestDto request);
    Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, CompleteInfoRequestDto request, int userId);
    Task<(bool Success, string Message)> ApplyFreeleechAsync(int torrentId, int userId);
    Task<FileStreamResult?> DownloadTorrentAsync(int torrentId);
    Task<Torrent?> GetTorrentByIdAsync(int torrentId);
    Task<(bool Success, string Message)> DeleteTorrentAsync(int torrentId, int userId);
}