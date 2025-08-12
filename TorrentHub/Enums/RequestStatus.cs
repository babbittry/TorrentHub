using System.Text.Json.Serialization;

namespace TorrentHub.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    Pending,   // The request is active and waiting to be filled.
    Filled,    // The request has been successfully filled.
}
