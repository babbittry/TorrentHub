using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class SiteSettingsDto
{
    // General
    [StringLength(50)]
    public string SiteName { get; set; } = "TorrentHub";

    [Url]
    public string? LogoUrl { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }

    // Registration
    public bool IsRegistrationOpen { get; set; } = false;

    // Features
    public bool IsRequestSystemEnabled { get; set; } = true;
    public bool IsForumEnabled { get; set; } = true;

    // Tracker
    [Url]
    public string TrackerUrl { get; set; } = "http://localhost:5001";

    [Range(300, 3600)]
    public int AnnounceIntervalSeconds { get; set; } = 1800;

    public bool GlobalFreeleechEnabled { get; set; } = false;

    // Announce Interval Control
    /// <summary>
    /// Minimum announce interval returned to clients (min interval in tracker response)
    /// </summary>
    [Range(60, 1800)]
    public int MinAnnounceIntervalSeconds { get; set; } = 900;  // 15 minutes

    /// <summary>
    /// Enforced minimum announce interval (server-side validation)
    /// Clients announcing more frequently will be rejected
    /// </summary>
    [Range(30, 900)]
    public int EnforcedMinAnnounceIntervalSeconds { get; set; } = 180;  // 3 minutes

    // Multi-Location Detection
    /// <summary>
    /// Enable detection of seeding from multiple locations
    /// </summary>
    public bool EnableMultiLocationDetection { get; set; } = true;

    /// <summary>
    /// Time window for detecting multi-location seeding (in minutes)
    /// </summary>
    [Range(1, 60)]
    public int MultiLocationDetectionWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Automatically log multi-location cheating attempts
    /// </summary>
    public bool LogMultiLocationCheating { get; set; } = true;

    // IP Change Tolerance
    /// <summary>
    /// Allow IP address changes (e.g., mobile networks, VPN switches)
    /// </summary>
    public bool AllowIpChange { get; set; } = true;

    /// <summary>
    /// Minimum interval for IP changes (in minutes)
    /// Changes shorter than this will be flagged as suspicious
    /// </summary>
    [Range(1, 60)]
    public int MinIpChangeIntervalMinutes { get; set; } = 10;

    // Speed Limits (in KB/s)
    [Range(0, 1024 * 125)]
    public int MaxUploadSpeed { get; set; } = 1024 * 125;

    [Range(0, 1024 * 125)]
    public int MaxDownloadSpeed { get; set; } = 1024 * 125;

    // Speed Check Configuration
    /// <summary>
    /// Minimum time interval for speed checks (in seconds)
    /// Announces shorter than this interval will not be checked for speed
    /// </summary>
    [Range(5, 300)]
    public int MinSpeedCheckIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Enable download speed checking
    /// </summary>
    public bool EnableDownloadSpeedCheck { get; set; } = true;

    // Cheat Detection
    /// <summary>
    /// Announce frequency threshold for cheat warnings (announces per minute)
    /// </summary>
    [Range(1, 100)]
    public int CheatWarningAnnounceThreshold { get; set; } = 20;

    /// <summary>
    /// Auto-ban after this many cheat warnings (0 = no auto-ban)
    /// </summary>
    [Range(0, 100)]
    public int AutoBanAfterCheatWarnings { get; set; } = 10;

    // Credential Cleanup
    /// <summary>
    /// Days of inactivity before credential is eligible for cleanup
    /// </summary>
    [Range(30, 365)]
    public int CredentialCleanupDays { get; set; } = 90;

    /// <summary>
    /// Enable automatic credential cleanup
    /// </summary>
    public bool EnableCredentialAutoCleanup { get; set; } = true;

    // Coins
    [Range(0, 1000000)]
    public uint InvitePrice { get; set; } = 5000;

    [Range(1, 365)]
    public int InviteExpirationDays { get; set; } = 30;

    [Range(0, 1000000)]
    public ulong CreateRequestCost { get; set; } = 1000;

    [Range(0, 1000000)]
    public ulong FillRequestBonus { get; set; } = 500;

    [Range(0, 100000)]
    public ulong CommentBonus { get; set; } = 10;
    
    [Range(0, 100000)]
    public ulong UploadTorrentBonus { get; set; } = 250;

    [Range(1, 100)]
    public int MaxDailyCommentBonuses { get; set; } = 10;

    [Range(0, 1.0)]
    public double TransactionTaxRate { get; set; } = 0.05;

    [Range(0, 1.0)]
    public double TipTaxRate { get; set; } = 0.10; // Tax rate for user-to-user tips

    [Range(0, 1.0)]
    public double TransferTaxRate { get; set; } = 0.05; // Tax rate for user-to-user coin transfers

    // Torrents
    public string TorrentStoragePath { get; set; } = "torrents"; // Relative to app root

    [Range(1024, 1024L * 1024 * 1024 * 100)]
    public long MaxTorrentSize { get; set; } = 1024L * 1024 * 1024 * 10; // 10 GB

    // Coin Generation
    [Range(1, 1440)]
    public int GenerationIntervalMinutes { get; set; } = 60;

    [Range(0.1, 100.0)]
    public double BaseGenerationRate { get; set; } = 1.0;

    [Range(0.0, 10.0)]
    public double SizeFactorMultiplier { get; set; } = 0.5;

    [Range(0.0, 10.0)]
    public double MosquitoFactorMultiplier { get; set; } = 0.1;

    [Range(0.0, 10.0)]
    public double SeederFactorMultiplier { get; set; } = 0.2;
}
