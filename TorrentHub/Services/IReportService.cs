using TorrentHub.Enums;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IReportService
{
    Task<(bool Success, string Message)> SubmitReportAsync(SubmitReportRequestDto request, int reporterUserId);
    Task<List<Report>> GetPendingReportsAsync();
    Task<List<Report>> GetProcessedReportsAsync();
    Task<(bool Success, string Message)> ProcessReportAsync(int reportId, int processedByUserId, ProcessReportRequestDto request);
}
