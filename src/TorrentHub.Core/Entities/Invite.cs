using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents an invitation code for user registration.
/// </summary>
public class Invite
{
    /// <summary>
    /// Unique identifier for the invite, also used as the invite code.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The actual invite code string.
    /// </summary>
    [Required]
    [StringLength(36)]
    public required string Code { get; set; }

    /// <summary>
    /// Foreign key of the user who generated the invite.
    /// </summary>
    [Required]
    public int GeneratorUserId { get; set; }

    /// <summary>
    /// Navigation property for the user who generated the invite.
    /// </summary>
    [ForeignKey(nameof(GeneratorUserId))]
    [InverseProperty(nameof(User.GeneratedInvites))]
    public User? GeneratorUser { get; set; }

    /// <summary>
    /// Navigation property for the user who used the invite.
    /// </summary>
    [InverseProperty(nameof(User.Invite))]
    public User? UsedByUser { get; set; }

    /// <summary>
    /// The date and time when the invite expires.
    /// </summary>
    [Required]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when the invite was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
