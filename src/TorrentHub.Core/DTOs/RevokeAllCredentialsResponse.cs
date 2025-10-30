namespace TorrentHub.Core.DTOs;

public class RevokeAllCredentialsResponse
{
    public int RevokedCount { get; set; }
    public List<int> AffectedTorrentIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}