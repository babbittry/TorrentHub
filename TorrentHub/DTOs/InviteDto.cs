namespace TorrentHub.DTOs;

public class InviteDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string GeneratorUsername { get; set; }
    public string? UsedByUsername { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
