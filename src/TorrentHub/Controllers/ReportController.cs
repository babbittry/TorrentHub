using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.Enums;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequestDto request)
    {
        var reporterUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _reportService.SubmitReportAsync(request, reporterUserId);

        if (!success)
        {
            _logger.LogWarning("Report submission failed: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<ActionResult<List<ReportDto>>> GetPendingReports()
    {
        var reports = await _reportService.GetPendingReportsAsync();
        return Ok(reports.Select(r => Mapper.ToReportDto(r)).ToList());
    }

    [HttpGet("processed")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<ActionResult<List<ReportDto>>> GetProcessedReports()
    {
        var reports = await _reportService.GetProcessedReportsAsync();
        return Ok(reports.Select(r => Mapper.ToReportDto(r)).ToList());
    }

    [HttpPatch("{reportId}/process")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> ProcessReport(int reportId, [FromBody] ProcessReportRequestDto request)
    {
        var processedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _reportService.ProcessReportAsync(reportId, processedByUserId, request);

        if (!success)
        {
            _logger.LogWarning("Report processing failed: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }
}
