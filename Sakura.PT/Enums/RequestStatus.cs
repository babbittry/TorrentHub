namespace Sakura.PT.Enums;

public enum RequestStatus
{
    Pending = 0,   // The request is active and waiting to be filled.
    Filled,    // The request has been successfully filled.
    Expired    // The request was not filled in time (if you add a time limit).
}
