using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Enums;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReport([FromForm] int torrentId, [FromForm] ReportReason reason, [FromForm] string? details)
    {
        var reporterUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _reportService.SubmitReportAsync(torrentId, reporterUserId, reason, details);

        if (!success)
        {
            _logger.LogWarning("Report submission failed: {Message}", message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> GetPendingReports()
    {
        var reports = await _reportService.GetPendingReportsAsync();
        return Ok(reports);
    }

    [HttpGet("processed")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> GetProcessedReports()
    {
        var reports = await _reportService.GetProcessedReportsAsync();
        return Ok(reports);
    }

    [HttpPost("{reportId}/process")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> ProcessReport(int reportId, [FromForm] string adminNotes, [FromForm] bool markAsProcessed)
    {
        var processedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _reportService.ProcessReportAsync(reportId, processedByUserId, adminNotes, markAsProcessed);

        if (!success)
        {
            _logger.LogWarning("Report processing failed: {Message}", message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }
}
