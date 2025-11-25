using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class UploadTorrentRequestDto
{
    [Required]
    public required IFormFile File { get; set; }

    /// <summary>
    /// 种子标题（用户自定义或覆盖 .torrent 文件名）
    /// </summary>
    [StringLength(255)]
    public string? Title { get; set; }

    /// <summary>
    /// 种子副标题/促销语
    /// </summary>
    [StringLength(255)]
    public string? Subtitle { get; set; }

    /// <summary>
    /// 用户提供的种子描述（富文本）
    /// </summary>
    [StringLength(4096)]
    public string? Description { get; set; }

    [Required]
    public TorrentCategory Category { get; set; }
    
    [StringLength(15)]
    public string? ImdbId { get; set; }

    /// <summary>
    /// TMDb ID（如果前端已获取，可减少一次 API 请求）
    /// </summary>
    public int? TMDbId { get; set; }

    /// <summary>
    /// 是否匿名发布
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// MediaInfo 文本
    /// </summary>
    [StringLength(8192)]
    public string? MediaInfo { get; set; }

    /// <summary>
    /// 技术规格（由前端提供）
    /// </summary>
    [Required]
    public required TechnicalSpecsDto TechnicalSpecs { get; set; }

    /// <summary>
    /// 截图文件列表（前端已压缩为 WebP 格式，建议恰好 3 张）
    /// </summary>
    public List<IFormFile>? Screenshots { get; set; }
}

