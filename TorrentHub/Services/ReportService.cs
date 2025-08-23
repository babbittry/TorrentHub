using Microsoft.EntityFrameworkCore;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SubmitReportAsync(SubmitReportRequestDto request, int reporterUserId)
    {
        var torrent = await _context.Torrents.FindAsync(request.TorrentId);
        if (torrent == null)
        {
            _logger.LogWarning("Report submission failed: Torrent {TorrentId} not found.", request.TorrentId);
            return (false, "Torrent not found.");
        }

        var reporter = await _context.Users.FindAsync(reporterUserId);
        if (reporter == null)
        {
            _logger.LogWarning("Report submission failed: Reporter user {ReporterUserId} not found.", reporterUserId);
            return (false, "Reporter user not found.");
        }

        // Prevent duplicate reports from the same user for the same torrent with the same reason (within a time frame, e.g., 24 hours)
        var existingReport = await _context.Reports
            .AnyAsync(r => r.TorrentId == request.TorrentId && r.ReporterUserId == reporterUserId && r.Reason == request.Reason && !r.IsProcessed && r.ReportedAt.AddHours(24) > DateTimeOffset.UtcNow);

        if (existingReport)
        {
            _logger.LogWarning("Report submission failed: Duplicate report from user {ReporterUserId} for torrent {TorrentId} with reason {Reason}.", reporterUserId, request.TorrentId, request.Reason);
            return (false, "You have recently reported this torrent for the same reason. Please wait before submitting another.");
        }

        var report = new Report
        {
            TorrentId = request.TorrentId,
            ReporterUserId = reporterUserId,
            Reason = request.Reason,
            Details = request.Details,
            ReportedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report submitted for torrent {TorrentId} by user {ReporterUserId} with reason {Reason}.", request.TorrentId, reporterUserId, request.Reason);
        return (true, "Report submitted successfully. Administrators will review it shortly.");
    }

    public async Task<List<Report>> GetPendingReportsAsync()
    {
        return await _context.Reports
            .Where(r => !r.IsProcessed)
            .Include(r => r.Torrent)
            .Include(r => r.ReporterUser)
            .OrderBy(r => r.ReportedAt)
            .ToListAsync();
    }

    public async Task<List<Report>> GetProcessedReportsAsync()
    {
        return await _context.Reports
            .Where(r => r.IsProcessed)
            .Include(r => r.Torrent)
            .Include(r => r.ReporterUser)
            .Include(r => r.ProcessedByUser)
            .OrderByDescending(r => r.ProcessedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> ProcessReportAsync(int reportId, int processedByUserId, ProcessReportRequestDto request)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null)
        {
            _logger.LogWarning("Report processing failed: Report {ReportId} not found.", reportId);
            return (false, "Report not found.");
        }

        if (report.IsProcessed)
        {
            _logger.LogWarning("Report processing failed: Report {ReportId} already processed.", reportId);
            return (false, "Report already processed.");
        }

        var processor = await _context.Users.FindAsync(processedByUserId);
        if (processor == null)
        {
            _logger.LogWarning("Report processing failed: Processor user {ProcessedByUserId} not found.", processedByUserId);
            return (false, "Processor user not found.");
        }

        report.IsProcessed = request.MarkAsProcessed;
        report.ProcessedByUserId = processedByUserId;
        report.ProcessedAt = DateTimeOffset.UtcNow;
        report.AdminNotes = request.AdminNotes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} processed by user {ProcessedByUserId}. Mark as processed: {MarkAsProcessed}.", reportId, processedByUserId, request.MarkAsProcessed);
        return (true, "Report processed successfully.");
    }
}
