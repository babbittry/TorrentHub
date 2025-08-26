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
    /// Nominal uploaded bytes, used for calculating share ratio (includes multipliers).
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong NominalUploadedBytes { get; set; }

    /// <summary>
    /// Nominal downloaded bytes, used for calculating share ratio (includes multipliers).
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong NominalDownloadedBytes { get; set; }

    public required Guid RssKey { get; set; }

    /// <summary>
    /// Unique key for announce URL, do not expose to other users.
    /// </summary>
    public required Guid Passkey { get; set; }

    /// <summary>
    /// Role of the user in the system.
    /// </summary>
    [Required]
    [DefaultValue(UserRole.User)]
    [Column(TypeName = "user_role")]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Bitmask representing the user's ban status. Allows for multiple, independent bans.
    /// </summary>
    [Required]
    [DefaultValue(BanStatus.None)]
    public BanStatus BanStatus { get; set; } = BanStatus.None;

    /// <summary>
    /// Textual reason for the ban, set by an administrator.
    /// </summary>
    [StringLength(50)]
    public string? BanReason { get; set; }

    /// <summary>
    /// A counter for cheating offenses, can be used for automatic sanctions.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int CheatWarningCount { get; set; } = 0;

    /// <summary>
    /// Date and time until the ban is active.
    /// </summary>
    public DateTimeOffset? BanUntil { get; set; }

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
    /// Total time (in minutes) the user has spent leeching torrents.
    /// </summary>
    [Required]
    [DefaultValue(0UL)]
    public ulong TotalLeechingTimeMinutes { get; set; } = 0UL;

    /// <summary>
    /// Indicates if the user's upload is currently doubled.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsDoubleUploadActive { get; set; } = false;

    /// <summary>
    /// The date and time when double upload status expires.
    /// </summary>
    public DateTimeOffset? DoubleUploadExpiresAt { get; set; }

    /// <summary>
    /// Indicates if the user is currently exempt from Hit & Run rules.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsNoHRActive { get; set; } = false;

    /// <summary>
    /// The date and time when No Hit & Run status expires.
    /// </summary>
    public DateTimeOffset? NoHRExpiresAt { get; set; }

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