using System.Text.Json.Serialization;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class StoreItemDto
{
    public int Id { get; set; }
    // We keep ItemCode for backend logic, but frontend will primarily use ActionType.
    [JsonIgnore]
    public StoreItemCode ItemCode { get; set; }
    
    public required string NameKey { get; set; }
    public required string DescriptionKey { get; set; }
    public ulong Price { get; set; }
    public bool IsAvailable { get; set; }

    // Use a string representation of the enum for frontend compatibility.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StoreActionType ActionType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ActionMetadata? ActionMetadata { get; set; }
}

