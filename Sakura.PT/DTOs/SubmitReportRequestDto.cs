using Sakura.PT.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class SubmitReportRequestDto
{
    [Required]
    public int TorrentId { get; set; }

    [Required]
    public ReportReason Reason { get; set; }

    public string? Details { get; set; }
}
