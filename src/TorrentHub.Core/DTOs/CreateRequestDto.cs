using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreateRequestDto
{
    [StringLength(100)]
    public required string Title { get; set; }

    [StringLength(500)]
    public required string Description { get; set; }
    public ulong InitialBounty { get; set; } = 0UL;
}
