using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class ProcessReportRequestDto
{
    [Required]
    public required string AdminNotes { get; set; }

    [Required]
    public bool MarkAsProcessed { get; set; }
}
