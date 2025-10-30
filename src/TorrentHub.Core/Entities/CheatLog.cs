
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    // 检测类型 (替代原有的Reason字段)
    [Required]
    [StringLength(100)]
    public required string DetectionType { get; set; }

    // IP地址
    [StringLength(45)] // IPv6最大长度
    public string? IpAddress { get; set; }

    // 保留原有字段以向后兼容
    [Required]
    [StringLength(255)]
    public required string Reason { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }
}
