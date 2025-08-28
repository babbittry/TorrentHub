using System.ComponentModel.DataAnnotations;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class SubmitReportRequestDto
{
    [Required]
    public int TorrentId { get; set; }

    [Required]
    public ReportReason Reason { get; set; }

    public string? Details { get; set; }
}

