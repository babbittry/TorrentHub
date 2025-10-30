namespace TorrentHub.Core.DTOs;

/// <summary>
/// Credential数据传输对象
/// </summary>
public class CredentialDto
{
    public int Id { get; set; }
    public Guid Credential { get; set; }
    public int TorrentId { get; set; }
    public string TorrentName { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    
    // Statistics
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? FirstUsedAt { get; set; }
    public int UsageCount { get; set; }
    public int AnnounceCount { get; set; }
    public ulong TotalUploadedBytes { get; set; }
    public ulong TotalDownloadedBytes { get; set; }
    public string? LastIpAddress { get; set; }
    public string? LastUserAgent { get; set; }
}