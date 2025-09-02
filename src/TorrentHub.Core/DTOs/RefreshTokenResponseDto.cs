namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents the data returned to the client after a successful token refresh.
/// </summary>
public class RefreshTokenResponseDto
{
    /// <summary>
    /// The new short-lived JWT access token.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// The user's private profile information.
    /// </summary>
    public required UserPrivateProfileDto User { get; set; }
}