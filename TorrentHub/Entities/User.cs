using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User's chosen username. Must be unique.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public required string UserName { get; set; }

    /// <summary>
    /// User's email address. Must be unique.
    /// </summary>
    [Required]
    [StringLength(255)]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// Hashed password for the user.
    /// </summary>
    [Required]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// URL to the user's avatar image.
    /// </summary>
    [StringLength(512)]
    public string? Avatar { get; set; }

    /// <summary>
    /// User's signature.
    /// </summary>
    [StringLength(500)]
    public string? Signature { get; set; }

    /// <summary>
    /// User's preferred language.
    /// </summary>
    [Required]
    [StringLength(10)]
    [DefaultValue("zh-CN")]
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// Total bytes uploaded by the user.
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong UploadedBytes { get; set; }

    /// <summary>
    /// Total bytes downloaded by the user.
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong DownloadedBytes { get; set; }
    
    /// <summary>
    /// Total bytes uploaded by the user (for display, considering multipliers).
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong DisplayUploadedBytes { get; set; }

    /// <summary>
    /// Total bytes downloaded by the user (for display, considering multipliers).
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong DisplayDownloadedBytes { get; set; }

    /// <summary>
    /// Unique key for RSS feed access.
    /// </summary>
    [StringLength(32)]
    public string? RssKey { get; set; }

    /// <summary>
    /// Unique passkey for tracker authentication.
    /// </summary>
    [Required]
    [StringLength(32)]
    public required string Passkey { get; set; }

    /// <summary>
    /// Role of the user in the system.
    /// </summary>
    [Required]
    [DefaultValue(UserRole.User)]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the user is banned.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsBanned { get; set; } = false;

    /// <summary>
    /// Reason for the ban.
    /// </summary>
    public UserBanReason? BanReason { get; set; }

    /// <summary>
    /// Date and time until the ban is active.
    /// </summary>
    public DateTime? BanUntil { get; set; }

    /// <summary>
    /// Number of invites the user can generate.
    /// </summary>
    [Required]
    [DefaultValue(0U)]
    public uint InviteNum { get; set; } = 0U;

    

    /// <summary>
    /// User's Coins balance.
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong Coins { get; set; } = 0UL;

    /// <summary>
    /// Total time (in minutes) the user has spent seeding torrents.
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong TotalSeedingTimeMinutes { get; set; } = 0UL;

    /// <summary>
    /// Indicates if the user's upload is currently doubled.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsDoubleUploadActive { get; set; } = false;

    /// <summary>
    /// The date and time when double upload status expires.
    /// </summary>
    public DateTime? DoubleUploadExpiresAt { get; set; }

    /// <summary>
    /// Indicates if the user is currently exempt from Hit & Run rules.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsNoHRActive { get; set; } = false;

    /// <summary>
    /// The date and time when No Hit & Run status expires.
    /// </summary>
    public DateTime? NoHRExpiresAt { get; set; }

    /// <summary>
    /// Foreign key to the invite that was used to register this user.
    /// </summary>
    public Guid? InviteId { get; set; }

    /// <summary>
    /// Navigation property for the invite used.
    /// </summary>
    [ForeignKey(nameof(InviteId))]
    [InverseProperty(nameof(Entities.Invite.UsedByUser))]
    public Invite? Invite { get; set; }

    /// <summary>
    /// Collection of torrents uploaded by this user.
    /// </summary>
    public ICollection<Torrent> Torrents { get; set; } = new List<Torrent>();

    /// <summary>
    /// Collection of invites generated by this user.
    /// </summary>
    public ICollection<Invite> GeneratedInvites { get; set; } = new List<Invite>();
}