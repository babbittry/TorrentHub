using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a download credential for a specific user and torrent.
/// This provides a more secure alternative to a global passkey.
/// </summary>
public class TorrentCredential
{
    /// <summary>
    /// Unique identifier for the credential entry.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The unique credential identifier (GUID) used in the announce URL.
    /// </summary>
    [Required]
    public required Guid Credential { get; set; }

    /// <summary>
    /// Foreign key of the associated user.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property for the user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    /// <summary>
    /// Foreign key of the associated torrent.
    /// </summary>
    [Required]
    public int TorrentId { get; set; }

    /// <summary>
    /// Navigation property for the torrent.
    /// </summary>
    [ForeignKey(nameof(TorrentId))]
    public required Torrent Torrent { get; set; }

    /// <summary&gth;
    /// Timestamp when the credential was created (i.e., when the user first downloaded the torrent).
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last announce using this credential.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Indicates if this credential has been revoked by an admin or system.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Timestamp when the credential was revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Reason for the revocation.
    /// </summary>
    [StringLength(200)]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// Number of times this credential has been used in an announce request.
    /// Useful for abuse detection.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int UsageCount { get; set; } = 0;
}