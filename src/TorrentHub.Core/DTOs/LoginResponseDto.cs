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
    /// The user's private profile information. This will be null if the login was not successful.
    /// </summary>
    public UserPrivateProfileDto? User { get; set; }
}