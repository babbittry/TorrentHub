using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class RequestDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public UserPublicProfileDto? RequestedByUser { get; set; }
    public UserPublicProfileDto? FilledByUser { get; set; }
    public int? FilledWithTorrentId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FilledAt { get; set; }
    public ulong BountyAmount { get; set; }
}
