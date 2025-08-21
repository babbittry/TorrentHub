using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TorrentHub.Enums;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Mappers;

namespace TorrentHub.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;
    private readonly CoinSettings _coinSettings;
    private readonly IEmailService _emailService;
    private readonly IDistributedCache _cache;

    // Cache key prefix for user badges
    private const string UserBadgesCacheKeyPrefix = "UserBadges:";
    // Cache duration (e.g., 1 hour)
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public UserService(ApplicationDbContext context, IConfiguration configuration, ILogger<UserService> logger, IOptions<CoinSettings> coinSettings, IEmailService emailService, IDistributedCache cache)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _coinSettings = coinSettings.Value;
        _emailService = emailService;
        _cache = cache;
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
        _logger.LogInformation("User {UserName} logged in successfully.", userForLoginDto.UserName);

                return new LoginResponseDto
        {
            Token = token,
            User = user
        };
    }

    private string GenerateJwtToken(User user)
    {
        _logger.LogDebug("Generating JWT token for user: {UserName}", user.UserName);
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key 'Jwt:Key' is not configured."));
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
            .FirstOrDefaultAsync(i => i.Code == userForRegistrationDto.InviteCode && i.ExpiresAt > DateTime.UtcNow);

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

        // Send registration confirmation email
        var subject = "Welcome to TorrentHub!";
        var body = $"Hello {user.UserName},<br><br>Welcome to TorrentHub! Your account has been successfully created.<br><br>Enjoy your time on our tracker!<br><br>The TorrentHub Team";
        await _emailService.SendEmailAsync(user.Email, subject, body);
        _logger.LogInformation("Sent welcome email to {Email} for user {UserName}.", user.Email, user.UserName);

        return user;
    }

    public async Task<bool> AddCoinsAsync(int userId, UpdateCoinsRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Could not find user with ID {UserId} to add Coins.", userId);
            return false;
        }

        user.Coins += request.Amount;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully added {Amount} Coins to user {UserId}. New balance: {Balance}", request.Amount, userId, user.Coins);
        return true;
    }

    public async Task<bool> TransferCoinsAsync(int fromUserId, int toUserId, ulong amount)
    {
        if (amount <= 0)
        {
            _logger.LogWarning("Transfer failed: Amount must be positive. FromUser: {FromUser}, ToUser: {ToUser}, Amount: {Amount}", fromUserId, toUserId, amount);
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var fromUser = await _context.Users.FindAsync(fromUserId);
            var toUser = await _context.Users.FindAsync(toUserId);

            if (fromUser == null || toUser == null)
            {
                _logger.LogWarning("Transfer failed: One or both users not found. FromUser: {FromUser}, ToUser: {ToUser}", fromUserId, toUserId);
                return false;
            }

            if (fromUser.Coins < amount)
            {
                _logger.LogWarning("Transfer failed: Insufficient funds. FromUser: {FromUser}, Balance: {Balance}, Amount: {Amount}", fromUserId, fromUser.Coins, amount);
                return false;
            }

            // Calculate tax
            var taxAmount = (ulong)Math.Ceiling(amount * _coinSettings.TransactionTaxRate);
            var actualTransferAmount = amount - taxAmount;

            fromUser.Coins -= amount; // Deduct full amount from sender
            toUser.Coins += actualTransferAmount; // Receiver gets amount minus tax

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully transferred {ActualAmount} Coins (tax: {Tax}) from user {FromUser} to user {ToUser}.", actualTransferAmount, taxAmount, fromUserId, toUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during Coin transfer. Rolling back transaction. FromUser: {FromUser}, ToUser: {ToUser}, Amount: {Amount}", fromUserId, toUserId, amount);
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<List<BadgeDto>> GetUserBadgesAsync(int userId)
    {
        var cacheKey = $"{UserBadgesCacheKeyPrefix}{userId}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving user badges for user {UserId} from cache.", userId);
            return JsonSerializer.Deserialize<List<BadgeDto>>(cachedData) ?? new List<BadgeDto>();
        }

        _logger.LogInformation("Cache miss for user badges for user {UserId}. Refreshing from DB.", userId);
        var badges = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.Badge!)
            .Select(b => new BadgeDto
            {
                Id = b.Id,
                Code = b.Code
            })
            .ToListAsync();
            
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(badges), _cacheOptions);
        return badges;
    }

    public async Task<User> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for profile update.", userId);
            throw new Exception("User not found.");
        }

        Mapper.MapTo(profileDto, user);

        await _context.SaveChangesAsync();
        _logger.LogInformation("User profile for {UserId} updated successfully.", userId);
        return user;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for password change.", userId);
            throw new Exception("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed for user {UserId}: Invalid current password.", userId);
            throw new Exception("Invalid current password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Password for user {UserId} changed successfully.", userId);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(int page, int pageSize, string? searchTerm)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        }

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<User> UpdateUserByAdminAsync(int userId, UpdateUserAdminDto updateUserAdminDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for admin update.", userId);
            throw new Exception("User not found.");
        }

        Mapper.MapTo(updateUserAdminDto, user);

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} updated by admin successfully.", userId);
        return user;
    }

    public async Task<IEnumerable<Invite>> GetUserInvitesAsync(int userId)
    {
        return await _context.Invites
            .Where(i => i.GeneratorUserId == userId)
            .Include(i => i.UsedByUser) // Include the user who used the invite
            .ToListAsync();
    }

    public async Task<Invite> GenerateInviteAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for invite generation.", userId);
            throw new Exception("User not found.");
        }

        // Check if user has enough Coins to generate an invite
        if (user.Coins < _coinSettings.InvitePrice)
        {
            _logger.LogWarning("User {UserId} does not have enough Coins to generate an invite. Required: {Required}, Has: {Has}", userId, _coinSettings.InvitePrice, user.Coins);
            throw new Exception("Insufficient Coins to generate an invite.");
        }

        // Deduct coins
        user.Coins -= _coinSettings.InvitePrice;

        var newInvite = new Invite
        {
            Code = Guid.NewGuid().ToString("N").Substring(0, 16),
            GeneratorUserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_coinSettings.InviteExpirationDays)
        };

        _context.Invites.Add(newInvite);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} generated a new invite code {InviteCode} for {Price} Coins.", userId, newInvite.Code, _coinSettings.InvitePrice);

        return newInvite;
    }

    public async Task<UserProfileDetailDto?> GetUserProfileDetailAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        // Get inviter
        string? invitedBy = null;
        if (user.InviteId.HasValue)
        {
            var invite = await _context.Invites
                .Include(i => i.GeneratorUser)
                .FirstOrDefaultAsync(i => i.Id == user.InviteId.Value);
            invitedBy = invite?.GeneratorUser?.UserName;
        }

        // Get peer stats
        var peerStats = await _context.Peers
            .Where(p => p.UserId == userId)
            .Include(p => p.Torrent)
            .GroupBy(p => p.IsSeeder)
            .Select(g => new
            {
                IsSeeder = g.Key,
                Count = g.Count(),
                Size = g.Sum(p => p.Torrent.Size)
            })
            .ToListAsync();

        var seedingCount = peerStats.FirstOrDefault(s => s.IsSeeder)?.Count ?? 0;
        var leechingCount = peerStats.FirstOrDefault(s => !s.IsSeeder)?.Count ?? 0;
        var seedingSize = (ulong)(peerStats.FirstOrDefault(s => s.IsSeeder)?.Size ?? 0L);

        // Manual mapping to DTO
        var profileDto = new UserProfileDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            UploadedBytes = user.UploadedBytes,
            DownloadedBytes = user.DownloadedBytes,
            NominalUploadedBytes = user.NominalUploadedBytes,
            NominalDownloadedBytes = user.NominalDownloadedBytes,
            Coins = user.Coins,
            TotalSeedingTimeMinutes = user.TotalSeedingTimeMinutes,
            TotalLeechingTimeMinutes = user.TotalLeechingTimeMinutes,
            InvitedBy = invitedBy,
            SeedingSize = seedingSize,
            CurrentSeedingCount = seedingCount,
            CurrentLeechingCount = leechingCount
        };

        return profileDto;
    }

    public async Task<IEnumerable<TorrentDto>> GetUserUploadsAsync(int userId)
    {
        var torrents = await _context.Torrents
            .Where(t => t.UploadedByUserId == userId)
            .Include(t => t.UploadedByUser)
            .ToListAsync();

        return torrents.Select(Mapper.ToTorrentDto);
    }

    public async Task<IEnumerable<PeerDto>> GetUserPeersAsync(int userId)
    {
        var peers = await _context.Peers
            .Where(p => p.UserId == userId)
            .Include(p => p.Torrent)
            .ToListAsync();

        return peers.Select(p => new PeerDto
        {
            TorrentId = p.TorrentId,
            TorrentName = p.Torrent.Name,
            UserAgent = "N/A", // Not stored in Peers entity
            IpAddress = p.IpAddress,
            Port = p.Port,
            Uploaded = 0, // Not stored in Peers entity
            Downloaded = 0, // Not stored in Peers entity
            IsSeeder = p.IsSeeder,
            LastAnnounceAt = p.LastAnnounce
        });
    }
}
