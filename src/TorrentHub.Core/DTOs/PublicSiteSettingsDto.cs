namespace TorrentHub.Core.DTOs;

/// <summary>
/// Contains site settings that are safe to be exposed to authenticated clients.
/// </summary>
public class PublicSiteSettingsDto
{
    // General
    public string SiteName { get; set; } = "TorrentHub";
    public bool IsRequestSystemEnabled { get; set; } = true;

    // Coins & Economy
    public ulong CreateRequestCost { get; set; } = 1000;
    public ulong FillRequestBonus { get; set; } = 500;
    public double TipTaxRate { get; set; } = 0.10;
    public double TransferTaxRate { get; set; } = 0.05;
    public uint InvitePrice { get; set; } = 5000;
}