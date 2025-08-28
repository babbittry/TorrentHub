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
    [Range(300, 3600)]
    public int AnnounceIntervalSeconds { get; set; } = 1800;

    public bool GlobalFreeleechEnabled { get; set; } = false;

    // Speed Limits (in KB/s)
    [Range(0, 1024 * 125)]
    public int MaxUploadSpeed { get; set; } = 1024 * 125;

    [Range(0, 1024 * 125)]
    public int MaxDownloadSpeed { get; set; } = 1024 * 125;

    // Coins
    [Range(0, 1000000)]
    public uint InvitePrice { get; set; } = 5000;

    [Range(1, 365)]
    public int InviteExpirationDays { get; set; } = 30;

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
