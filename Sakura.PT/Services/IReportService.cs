using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public interface IReportService
{
    Task<(bool Success, string Message)> SubmitReportAsync(int torrentId, int reporterUserId, ReportReason reason, string? details);
    Task<List<Report>> GetPendingReportsAsync();
    Task<List<Report>> GetProcessedReportsAsync();
    Task<(bool Success, string Message)> ProcessReportAsync(int reportId, int processedByUserId, string adminNotes, bool markAsProcessed);
}
