namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents the data returned to the client after a successful login.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Indicates if the login process requires a second factor of authentication.
    /// If true, the client should prompt the user for a 2FA code.
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = false;

    /// <summary>
    /// The short-lived JWT access token. This will be null if <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The user's private profile information. This will be null if <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public UserPrivateProfileDto? User { get; set; }
}