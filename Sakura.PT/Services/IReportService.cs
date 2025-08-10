using Sakura.PT.Entities;
using Sakura.PT.Enums;
using Sakura.PT.DTOs;

namespace Sakura.PT.Services;

public interface IReportService
{
    Task<(bool Success, string Message)> SubmitReportAsync(SubmitReportRequestDto request, int reporterUserId);
    Task<List<Report>> GetPendingReportsAsync();
    Task<List<Report>> GetProcessedReportsAsync();
    Task<(bool Success, string Message)> ProcessReportAsync(int reportId, int processedByUserId, ProcessReportRequestDto request);
}
