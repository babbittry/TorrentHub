namespace Sakura.PT.Services;

public class SakuraCoinSettings
{
    public int GenerationIntervalMinutes { get; set; } = 30;
    public double BaseGenerationRate { get; set; } = 1.0;
    public double SizeFactorMultiplier { get; set; } = 0.5;
    public double LeecherFactorMultiplier { get; set; } = 1.5;
    public double SeederFactorMultiplier { get; set; } = 0.8;
    public long UploadTorrentBonus { get; set; } = 50;
    public long FillRequestBonus { get; set; } = 100;
    public long CommentBonus { get; set; } = 5;
    public long CompleteInfoBonus { get; set; } = 20;
    public int MaxDailyCommentBonuses { get; set; } = 5;
    public long FreeleechTokenPrice { get; set; } = 50000;
    public int FreeleechTokenDurationHours { get; set; } = 24;
    public double TransactionTaxRate { get; set; } = 0.02; // 2%
}
