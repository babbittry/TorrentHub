using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Entities;

public class CoinTransaction
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column(TypeName = "transaction_type")]
    public TransactionType Type { get; set; }

    public int? FromUserId { get; set; } // 可空，因为系统发放时没有来源用户

    [ForeignKey(nameof(FromUserId))]
    public User? FromUser { get; set; }

    public int? ToUserId { get; set; } // 可空，因为系统收费时没有目标用户

    [ForeignKey(nameof(ToUserId))]
    public User? ToUser { get; set; }

    [Required]
    public ulong Amount { get; set; } // 交易的原始金额

    [Required]
    public ulong TaxAmount { get; set; } = 0; // 系统从中收取的税费

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(200)]
    public string? Notes { get; set; } // 备注，例如关联的实体ID (e.g., "torrent:123", "user:456")
}