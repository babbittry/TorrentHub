using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class StoreItemDto
{
    public int Id { get; set; }
    public StoreItemCode ItemCode { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ulong Price { get; set; }
    public bool IsAvailable { get; set; }
    public int? BadgeId { get; set; }
}
