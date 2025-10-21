using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendVerificationEmailAsync(string userNameOrEmail);
    Task<(LoginResponseDto Dto, string? RefreshToken)> LoginAsync(UserForLoginDto userForLoginDto);
    Task<(string AccessToken, string RefreshToken, User User)> Login2faAsync(UserForLogin2faDto login2faDto);
    Task<(string AccessToken, User User)?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task SendLoginVerificationEmailAsync(string userName);
    Task<(string ManualEntryKey, string QrCodeImageUrl)> GenerateTwoFactorSetupAsync(int userId);
    Task<bool> SwitchToAuthenticatorAppAsync(int userId, string code);
    Task<bool> SwitchToEmailAsync(int userId, string code);
    Task<bool> AddCoinsAsync(int userId, UpdateCoinsRequestDto request);
    Task<(bool Success, string Message)> TransferCoinsAsync(int fromUserId, int toUserId, ulong amount, string? notes);
    Task<(bool Success, string Message)> TipCoinsAsync(int fromUserId, int toUserId, ulong amount, string? notes);
    Task<User?> GetUserByIdAsync(int userId);
    Task<int> GetUnreadMessagesCountAsync(int userId);
    Task<UserPublicProfileDto?> GetUserPublicProfileAsync(int userId);
    Task<List<BadgeDto>> GetUserBadgesAsync(int userId);
    Task<User> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<IEnumerable<User>> GetUsersAsync(int page, int pageSize, string? searchTerm);
    Task<User> UpdateUserByAdminAsync(int userId, UpdateUserAdminDto updateUserAdminDto);
    Task<IEnumerable<Invite>> GetUserInvitesAsync(int userId);
    Task<Invite> GenerateInviteAsync(int userId, bool chargeForInvite = true);
    Task UpdateUserAsync(User user);
    Task<IEnumerable<TorrentDto>> GetUserUploadsAsync(int userId);
    Task<IEnumerable<PeerDto>> GetUserPeersAsync(int userId);
    Task EquipBadgeAsync(int userId, int badgeId);
    Task UpdateUserTitleAsync(int userId, string newTitle);
}
