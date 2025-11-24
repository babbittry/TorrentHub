namespace TorrentHub.Core.DTOs;

/// <summary>
/// 文件上传响应
/// </summary>
public class UploadFileResponseDto
{
    /// <summary>
    /// 文件的公共访问 URL
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// 文件名 (服务器生成的唯一文件名)
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// 文件大小 (字节)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public required string ContentType { get; set; }
}