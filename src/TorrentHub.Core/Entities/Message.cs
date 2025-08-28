using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a private message between users.
/// </summary>
public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SenderId { get; set; }

    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    [Required]
    public int ReceiverId { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public User? Receiver { get; set; }

    [Required]
    [StringLength(200)]
    public required string Subject { get; set; }

    [Required]
    [StringLength(500)]
    public required string Content { get; set; }

    [Required]
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Indicates if the message is deleted by the sender.
    /// </summary>
    [Required]
    public bool SenderDeleted { get; set; } = false;

    /// <summary>
    /// Indicates if the message is deleted by the receiver.
    /// </summary>
    [Required]
    public bool ReceiverDeleted { get; set; } = false;
}
