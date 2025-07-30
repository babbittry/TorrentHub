using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface IUserService
{
    Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto);
    Task<LoginResponseDto> LoginAsync(UserForLoginDto userForLoginDto);
    Task<bool> AddSakuraCoinsAsync(int userId, long amount);
    Task<bool> TransferSakuraCoinsAsync(int fromUserId, int toUserId, long amount);
}
