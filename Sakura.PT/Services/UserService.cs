using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;
using Sakura.PT.Mappers;
using Microsoft.Extensions.Logging;

namespace Sakura.PT.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext context, IConfiguration configuration, ILogger<UserService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<LoginResponseDto> LoginAsync(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Attempting login for user: {UserName}", userForLoginDto.UserName);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userForLoginDto.UserName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(userForLoginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for user {UserName}: Invalid credentials.", userForLoginDto.UserName);
            throw new Exception("Invalid username or password.");
        }

        var token = GenerateJwtToken(user);
        var userDto = Mapper.ToUserDto(user);
        _logger.LogInformation("User {UserName} logged in successfully.", userForLoginDto.UserName);

        return new LoginResponseDto
        {
            Token = token,
            User = userDto
        };
    }

    private string GenerateJwtToken(User user)
    {
        _logger.LogDebug("Generating JWT token for user: {UserName}", user.UserName);
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        _logger.LogDebug("JWT token generated for user: {UserName}", user.UserName);
        return tokenHandler.WriteToken(token);
    }

    public async Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto)
    {
        _logger.LogInformation("Attempting registration for user: {UserName}", userForRegistrationDto.UserName);
        // 1. Validate Invite Code
        var invite = await _context.Invites
            .FirstOrDefaultAsync(i => i.Code == userForRegistrationDto.InviteCode && i.UsedByUserId == null && i.ExpiresAt > DateTime.UtcNow);

        if (invite == null)
        {
            _logger.LogWarning("Registration failed for user {UserName}: Invalid or expired invite code {InviteCode}.", userForRegistrationDto.UserName, userForRegistrationDto.InviteCode);
            throw new Exception("Invalid or expired invite code.");
        }

        // 2. Check for duplicate username or email
        if (await _context.Users.AnyAsync(u => u.UserName == userForRegistrationDto.UserName))
        {
            _logger.LogWarning("Registration failed for user {UserName}: Username already exists.", userForRegistrationDto.UserName);
            throw new Exception("Username already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == userForRegistrationDto.Email))
        {
            _logger.LogWarning("Registration failed for user {UserName}: Email {Email} already exists.", userForRegistrationDto.UserName, userForRegistrationDto.Email);
            throw new Exception("Email already exists.");
        }

        // 3. Create new user
        var user = new User
        {
            UserName = userForRegistrationDto.UserName,
            Email = userForRegistrationDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userForRegistrationDto.Password), // Hashing the password
            InviteId = invite.Id,
            CreatedAt = DateTime.UtcNow,
            Passkey = Guid.NewGuid().ToString("N") // Generate a new Passkey
        };

        // 4. Update invite and save changes
        invite.UsedByUser = user;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserName} registered successfully with ID {UserId}.", user.UserName, user.Id);

        return user;
    }
}
