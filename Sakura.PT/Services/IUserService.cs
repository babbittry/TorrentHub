using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto);
    Task<LoginResponseDto> LoginAsync(UserForLoginDto userForLoginDto);
    Task<bool> AddSakuraCoinsAsync(int userId, UpdateSakuraCoinsRequestDto request);
    Task<bool> TransferSakuraCoinsAsync(int fromUserId, int toUserId, ulong amount);
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<Badge>> GetUserBadgesAsync(int userId);
    Task<User> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<IEnumerable<User>> GetUsersAsync(int page, int pageSize, string? searchTerm);
    Task<User> UpdateUserByAdminAsync(int userId, UpdateUserAdminDto updateUserAdminDto);
    Task<IEnumerable<Invite>> GetUserInvitesAsync(int userId);
    Task<Invite> GenerateInviteAsync(int userId);
}
