using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a refresh token for a user, enabling persistent sessions.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the token.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The actual token value. This is the value sent to the client.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string Token { get; set; }

    /// <summary>
    /// The hash of the token, stored in the database for security.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string TokenHash { get; set; }

    /// <summary>
    /// Foreign key to the User who owns this token.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the User.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    /// <summary>
    /// The date and time when this token expires and is no longer valid.
    /// </summary>
    [Required]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// The date and time when this token was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time when this token was revoked (e.g., due to logout).
    /// If not null, the token is no longer valid.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Checks if the token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Checks if the token is currently active and valid.
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}