namespace TorrentHub.Services.Configuration;

public class CredentialSettings
{
    /// <summary>
    /// 清理任务运行间隔(天数)
    /// </summary>
    public int CleanupIntervalDays { get; set; } = 1;
}