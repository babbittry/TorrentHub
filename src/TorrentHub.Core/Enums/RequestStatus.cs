using System.Text.Json.Serialization;

namespace TorrentHub.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    Pending,   // The request is active and waiting to be filled.
    Filled,    // The request has been successfully filled.
    PendingConfirmation, // A user has filled the request, waiting for requester's confirmation
    Rejected,  // The requester has rejected the filled torrent. The request is active again.
}
