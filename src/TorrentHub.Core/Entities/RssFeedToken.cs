using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents an RSS feed token for secure RSS subscription access
/// </summary>
public class RssFeedToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid Token { get; set; }

    [Required]
    public int UserId { get; set; }
    
    public required User User { get; set; }

    // Permission Configuration
    /// <summary>
    /// Type of RSS feed (Latest, Category, Bookmarks, Custom)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string FeedType { get; set; }

    /// <summary>
    /// Array of category filters (e.g., ["Movie", "TV"])
    /// </summary>
    public string[]? CategoryFilter { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 50;

    // Token Status
    /// <summary>
    /// Whether the token is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Token expiration date (null = never expires)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Last time the token was used
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this token has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    // Metadata
    /// <summary>
    /// User-defined name for this token (e.g., "Mobile RSS Reader")
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// User-Agent string from last use
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// IP address from last use
    /// </summary>
    [MaxLength(45)]
    public string? LastIp { get; set; }

    /// <summary>
    /// When this token was created
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this token was revoked (if applicable)
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}