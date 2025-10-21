namespace TorrentHub.Core.DTOs;

/// <summary>
/// 种子上传成功响应 DTO
/// </summary>
public class UploadTorrentResponseDto
{
    /// <summary>
    /// 新创建的种子 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 种子名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 种子分类
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 种子大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}