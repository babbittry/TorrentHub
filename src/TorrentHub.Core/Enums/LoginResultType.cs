namespace TorrentHub.Core.Enums;

public enum LoginResultType
{
    Success,
    InvalidCredentials,
    EmailNotVerified,
    Banned,
    RequiresTwoFactor
}