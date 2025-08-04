namespace Sakura.PT.Services;

public class SakuraCoinSettings
{
    public int GenerationIntervalMinutes { get; set; } = 30;
    public double BaseGenerationRate { get; set; } = 1.0;
    public double SizeFactorMultiplier { get; set; } = 0.5;
    public double MosquitoFactorMultiplier { get; set; } = 1.5;
    public double SeederFactorMultiplier { get; set; } = 0.8;
    public ulong UploadTorrentBonus { get; set; } = 50UL;
    public ulong FillRequestBonus { get; set; } = 100UL;
    public ulong CommentBonus { get; set; } = 5UL;
    public ulong CompleteInfoBonus { get; set; } = 20UL;
    public int MaxDailyCommentBonuses { get; set; } = 5;
    public ulong FreeleechPrice { get; set; } = 50000UL;
    public int FreeleechDurationHours { get; set; } = 24;
    public double TransactionTaxRate { get; set; } = 0.1; // 10%

    // H&R Exemption Durations (in hours) per UserRole
    public int UserHRExemptionHours { get; set; } = 24; // Default for User
    public int PowerUserHRExemptionHours { get; set; } = 48;
    public int EliteUserHRExemptionHours { get; set; } = 72;
    public int CrazyUserHRExemptionHours { get; set; } = 96;
    public int VeteranUserHRExemptionHours { get; set; } = 120;
    public int VIPHRExemptionHours { get; set; } = 168; // 1 week
}
