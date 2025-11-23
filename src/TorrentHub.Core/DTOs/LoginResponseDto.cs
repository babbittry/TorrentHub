using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents the data returned to the client after a login attempt.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// The result of the login attempt.
    /// </summary>
    public LoginResultType Result { get; set; }

    /// <summary>
    /// The short-lived JWT access token. This will be null if the login was not successful.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The user's private profile information. Only populated when login is successful (Result = Success).
    /// </summary>
    public UserPrivateProfileDto? User { get; set; }

    /// <summary>
    /// Minimal user information for 2FA pending state. Only populated when Result = RequiresTwoFactor.
    /// Contains only necessary fields like userName, masked email, and twoFactorMethod.
    /// </summary>
    public UserPending2faDto? Pending2faUser { get; set; }
}