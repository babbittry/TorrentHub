namespace TorrentHub.Core.DTOs;

public class RevokeBatchRequest
{
    public required Guid[] CredentialIds { get; set; }
    public string? Reason { get; set; }
}