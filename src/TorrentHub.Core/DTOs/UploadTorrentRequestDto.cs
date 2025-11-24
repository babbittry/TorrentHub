using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class UploadTorrentRequestDto
{
    [Required]
    public required IFormFile File { get; set; }

    [StringLength(4096)]
    public string? Description { get; set; }

    [Required]
    public TorrentCategory Category { get; set; }
    
    [StringLength(15)]
    public string? ImdbId { get; set; }

    /// <summary>
    /// 截图文件列表（前端已压缩为 WebP 格式，建议恰好 3 张）
    /// </summary>
    public List<IFormFile>? Screenshots { get; set; }
}

