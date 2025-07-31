using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SubmitReportAsync(int torrentId, int reporterUserId, ReportReason reason, string? details)
    {
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            _logger.LogWarning("Report submission failed: Torrent {TorrentId} not found.", torrentId);
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
            .AnyAsync(r => r.TorrentId == torrentId && r.ReporterUserId == reporterUserId && r.Reason == reason && !r.IsProcessed && r.ReportedAt.AddHours(24) > DateTime.UtcNow);

        if (existingReport)
        {
            _logger.LogWarning("Report submission failed: Duplicate report from user {ReporterUserId} for torrent {TorrentId} with reason {Reason}.", reporterUserId, torrentId, reason);
            return (false, "You have recently reported this torrent for the same reason. Please wait before submitting another.");
        }

        var report = new Report
        {
            TorrentId = torrentId,
            ReporterUserId = reporterUserId,
            Reason = reason,
            Details = details,
            ReportedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report submitted for torrent {TorrentId} by user {ReporterUserId} with reason {Reason}.", torrentId, reporterUserId, reason);
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

    public async Task<(bool Success, string Message)> ProcessReportAsync(int reportId, int processedByUserId, string adminNotes, bool markAsProcessed)
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

        report.IsProcessed = markAsProcessed;
        report.ProcessedByUserId = processedByUserId;
        report.ProcessedAt = DateTime.UtcNow;
        report.AdminNotes = adminNotes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} processed by user {ProcessedByUserId}. Mark as processed: {MarkAsProcessed}.", reportId, processedByUserId, markAsProcessed);
        return (true, "Report processed successfully.");
    }
}
