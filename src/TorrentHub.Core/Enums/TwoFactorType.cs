namespace TorrentHub.Core.Enums;

/// <summary>
/// Specifies the type of two-factor authentication a user has configured.
/// </summary>
public enum TwoFactorType
{
    /// <summary>
    /// 2FA is handled via codes sent to the user's registered email address.
    /// This is the default for new users.
    /// </summary>
    Email = 0,

    /// <summary>
    /// 2FA is handled via a Time-based One-Time Password (TOTP) application.
    /// </summary>
    AuthenticatorApp = 1,
}