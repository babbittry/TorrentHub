using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto);
    Task<LoginResponseDto> LoginAsync(UserForLoginDto userForLoginDto);
    Task<bool> AddCoinsAsync(int userId, UpdateCoinsRequestDto request);
    Task<bool> TransferCoinsAsync(int fromUserId, int toUserId, ulong amount);
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<BadgeDto>> GetUserBadgesAsync(int userId);
    Task<User> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<IEnumerable<User>> GetUsersAsync(int page, int pageSize, string? searchTerm);
    Task<User> UpdateUserByAdminAsync(int userId, UpdateUserAdminDto updateUserAdminDto);
    Task<IEnumerable<Invite>> GetUserInvitesAsync(int userId);
    Task<Invite> GenerateInviteAsync(int userId);

    Task UpdateUserAsync(User user);

    Task<UserProfileDetailDto?> GetUserProfileDetailAsync(int userId);
    Task<IEnumerable<TorrentDto>> GetUserUploadsAsync(int userId);
    Task<IEnumerable<PeerDto>> GetUserPeersAsync(int userId);
}
