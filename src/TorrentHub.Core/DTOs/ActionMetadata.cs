using System.Text.Json.Serialization;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// Provides configuration metadata to the frontend for rendering UI components 
/// related to a specific store item action.
/// </summary>
public class ActionMetadata
{
    // For PurchaseWithQuantity
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Min { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Max { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Step { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnitKey { get; set; } // e.g., "unit.gb", "unit.item"

    // For ChangeUsername
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InputLabelKey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlaceholderKey { get; set; }

    // For PurchaseBadge
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BadgeId { get; set; }
}