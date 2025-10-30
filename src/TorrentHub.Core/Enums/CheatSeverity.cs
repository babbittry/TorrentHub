namespace TorrentHub.Core.Enums;

/// <summary>
/// Represents the severity of a detected cheat.
/// </summary>
public enum CheatSeverity
{
    /// <summary>
    /// Low severity, potentially a false positive or minor issue.
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Medium severity, requires attention.
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// High severity, clear cheating behavior.
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Critical severity, malicious attack, requires immediate action.
    /// </summary>
    Critical = 4
}