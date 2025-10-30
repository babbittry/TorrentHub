
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Entities;

public class CheatLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // 种子相关信息
    public int? TorrentId { get; set; }

    [ForeignKey(nameof(TorrentId))]
    public Torrent? Torrent { get; set; }

    [Required]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // 检测类型
    [Required]
    public CheatDetectionType DetectionType { get; set; }

    // 严重等级
    [Required]
    public CheatSeverity Severity { get; set; } = CheatSeverity.Medium;

    // IP地址
    [StringLength(45)] // IPv6最大长度
    public string? IpAddress { get; set; }

    [StringLength(500)]
    /// <summary>
    /// 是否已处理
    /// </summary>
    [Required]
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// 处理的管理员用户ID
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    /// <summary>
    /// 处理的管理员 (导航属性)
    /// </summary>
    [ForeignKey(nameof(ProcessedByUserId))]
    public User? ProcessedByUser { get; set; }

    /// <summary>
    /// 管理员备注
    /// </summary>
    [StringLength(500)]
    public string? AdminNotes { get; set; }
    public string? Details { get; set; }
}
