namespace TorrentHub.DTOs;

public class InviteDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string GeneratorUsername { get; set; }
    public string? UsedByUsername { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
