namespace TorrentHub.Core.Services;

/// <summary>
/// 种子清洗服务接口
/// </summary>
public interface ITorrentWashingService
{
    /// <summary>
    /// 清洗种子文件
    /// </summary>
    /// <param name="torrentBytes">原始种子文件字节数组</param>
    /// <param name="torrentId">种子ID（用于生成 comment）</param>
    /// <param name="trackerUrl">新的 Tracker URL（从 SiteSettings 获取）</param>
    /// <param name="originalFileName">原始种子文件名</param>
    /// <returns>清洗后的种子文件字节数组</returns>
    byte[] WashTorrent(byte[] torrentBytes, int torrentId, string trackerUrl, string originalFileName);
    
    /// <summary>
    /// 计算种子的 InfoHash
    /// </summary>
    /// <param name="torrentBytes">种子文件字节数组</param>
    /// <returns>40位十六进制 InfoHash</returns>
    string CalculateInfoHash(byte[] torrentBytes);
    
    /// <summary>
    /// 生成清洗后的种子文件名
    /// </summary>
    /// <param name="originalFileName">原始种子文件名</param>
    /// <returns>替换了站点标识的新文件名</returns>
    string GenerateWashedFileName(string originalFileName);
}