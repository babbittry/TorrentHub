namespace TorrentHub.Core.DTOs;

/// <summary>
/// Credential数据传输对象
/// </summary>
public class CredentialDto
{
    /// <summary>
    /// Credential ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Credential UUID
    /// </summary>
    public Guid Credential { get; set; }
}