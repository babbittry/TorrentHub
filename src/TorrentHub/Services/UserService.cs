using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Hosting;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Mappers;
using TorrentHub.Core.Services;
using TorrentHub.Services.Interfaces;
using Google.Authenticator;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.IO;

namespace TorrentHub.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IDistributedCache _cache;
    private readonly ISettingsService _settingsService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IEmailService _emailService;
    private readonly IDataProtector _dataProtector;
    private static readonly Random _random = new();

    private const string DataProtectorPurpose = "2FASecretKey";
    private const string EmailVerificationTokenPurpose = "EmailVerification";

    public UserService(
        ApplicationDbContext context, 
        IConfiguration configuration, 
        ILogger<UserService> logger, 
        IDistributedCache cache, 
        ISettingsService settingsService, 
        IWebHostEnvironment webHostEnvironment,
        INotificationService notificationService,
        IEmailService emailService,
        IDataProtectionProvider dataProtectionProvider) 
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _settingsService = settingsService;
        _webHostEnvironment = webHostEnvironment;
        _notificationService = notificationService;
        _emailService = emailService;
        _dataProtector = dataProtectionProvider.CreateProtector(DataProtectorPurpose);
    }

    public async Task<User> RegisterAsync(UserForRegistrationDto userForRegistrationDto)
    {
        _logger.LogInformation("Attempting registration for user: {UserName}", userForRegistrationDto.UserName);
        
        Invite? invite = null;
        var settings = await _settingsService.GetSiteSettingsAsync();

        if (!string.IsNullOrEmpty(userForRegistrationDto.InviteCode))
        {
            invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.Code == userForRegistrationDto.InviteCode && i.ExpiresAt > DateTimeOffset.UtcNow && i.UsedByUser == null);

            if (invite == null)
            {
                throw new Exception("Invalid, expired, or already used invite code.");
            }
        }
        else
        {
            if (!settings.IsRegistrationOpen)
            {
                throw new Exception("Registration is currently by invitation only. An invite code is required.");
            }
        }

        if (await _context.Users.AnyAsync(u => u.UserName == userForRegistrationDto.UserName))
        {
            throw new Exception("Username already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == userForRegistrationDto.Email))
        {
            throw new Exception("Email already exists.");
        }

        var user = new User
        {
            UserName = userForRegistrationDto.UserName,
            Email = userForRegistrationDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userForRegistrationDto.Password),
            InviteId = invite?.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Passkey = Guid.NewGuid(),
            RssKey = Guid.NewGuid(),
            IsEmailVerified = false,
            TwoFactorType = TwoFactorType.Email,
            Language = userForRegistrationDto.Language ?? "zh-CN"
        };
        
        if (invite != null)
        {
            invite.UsedByUser = user;
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserName} created with ID {UserId}, awaiting email verification.", user.UserName, user.Id);

        var token = await GenerateEmailVerificationToken(user.Id);
        await _emailService.SendEmailVerificationLinkAsync(user, token);

        if (!string.IsNullOrEmpty(userForRegistrationDto.AvatarSvg))
        {
            try
            {
                var avatarDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "avatars");
                if (!Directory.Exists(avatarDirectory))
                {
                    Directory.CreateDirectory(avatarDirectory);
                }

                var avatarFileName = $"{Guid.NewGuid():N}.svg";
                var avatarFilePath = Path.Combine(avatarDirectory, avatarFileName);
                await File.WriteAllTextAsync(avatarFilePath, userForRegistrationDto.AvatarSvg);

                user.Avatar = $"/avatars/{avatarFileName}";
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save avatar for user {UserName}.", user.UserName);
            }
        }

        return user;
    }

    private async Task<string> GenerateEmailVerificationToken(int userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var cacheKey = $"{EmailVerificationTokenPurpose}:{token}";
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        await _cache.SetStringAsync(cacheKey, userId.ToString(), cacheOptions);
        return token;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var cacheKey = $"{EmailVerificationTokenPurpose}:{token}";
        var userIdString = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("Invalid email verification token received.");
            return false;
        }

        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("Email verification token was valid, but user {UserId} was not found.", userId);
            return false;
        }

        user.IsEmailVerified = true;
        await UpdateUserAsync(user);

        await _cache.RemoveAsync(cacheKey);
        await _notificationService.SendWelcomeEmailAsync(user);

        _logger.LogInformation("Email verified successfully for user {UserId}.", userId);
        return true;
    }

    public async Task<(LoginResponseDto Dto, string? RefreshToken)> LoginAsync(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Attempting login for user: {UserNameOrEmail}", userForLoginDto.UserNameOrEmail);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userForLoginDto.UserNameOrEmail || u.Email == userForLoginDto.UserNameOrEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(userForLoginDto.Password, user.PasswordHash))
        {
            return (new LoginResponseDto { Result = LoginResultType.InvalidCredentials }, null);
        }

        if (!user.IsEmailVerified)
        {
            return (new LoginResponseDto { Result = LoginResultType.EmailNotVerified }, null);
        }

        if (user.BanStatus.HasFlag(BanStatus.LoginBan))
        {
            return (new LoginResponseDto { Result = LoginResultType.Banned }, null);
        }

        if (user.TwoFactorType == TwoFactorType.Email)
        {
            await SendLoginVerificationEmailAsync(user.UserName);
        }

        return (new LoginResponseDto { Result = LoginResultType.RequiresTwoFactor }, null);
    }

    public async Task<(string AccessToken, string RefreshToken, User User)> Login2faAsync(UserForLogin2faDto login2faDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == login2faDto.UserName) 
                   ?? throw new Exception("Invalid user or verification code.");

        var totpValid = user.TwoFactorType == TwoFactorType.AuthenticatorApp && await ValidateTwoFactorCodeAsync(user, login2faDto.Code);
        var emailCodeValid = await ValidateEmailLoginCodeAsync(user.Id, login2faDto.Code);

        if (!totpValid && !emailCodeValid)
        {
            throw new Exception("Invalid verification code.");
        }

        var accessToken = GenerateJwtToken(user, TimeSpan.FromMinutes(15));
        var refreshToken = GenerateRefreshToken(user.Id);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserName} completed 2FA login successfully.", login2faDto.UserName);
        return (accessToken, refreshToken.Token, user);
    }

    public async Task<(string AccessToken, User User)?> RefreshTokenAsync(string refreshToken)
    {
        var refreshTokenHash = GetHash(refreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null || !storedToken.IsActive)
        {
            return null;
        }

        var newAccessToken = GenerateJwtToken(storedToken.User, TimeSpan.FromMinutes(15));
        return (newAccessToken, storedToken.User);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var refreshTokenHash = GetHash(refreshToken);
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
        {
            return false;
        }

        _context.RefreshTokens.Remove(storedToken);
        await _context.SaveChangesAsync();
        return true;
    }

    private string GenerateJwtToken(User user, TimeSpan lifetime)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.Add(lifetime),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(int userId)
    {
        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenHash = GetHash(refreshTokenString);

        return new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenString,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string GetHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public async Task<bool> AddCoinsAsync(int userId, UpdateCoinsRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.Coins += request.Amount;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message)> TransferCoinsAsync(int fromUserId, int toUserId, ulong amount, string? notes)
    {
        if (fromUserId == toUserId) return (false, "error.transfer.self");
        if (amount == 0) return (false, "error.transfer.zeroAmount");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fromUser = await _context.Users.FindAsync(fromUserId);
            var toUser = await _context.Users.FindAsync(toUserId);

            if (fromUser == null || toUser == null) return (false, "error.user.notFound");
            if (fromUser.Coins < amount) return (false, "error.transfer.insufficientCoins");

            var settings = await _settingsService.GetSiteSettingsAsync();
            var tax = (ulong)(amount * settings.TransferTaxRate);
            var receivedAmount = amount - tax;

            fromUser.Coins -= amount;
            toUser.Coins += receivedAmount;

            var coinTransaction = new CoinTransaction
            {
                Type = TransactionType.Transfer,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Amount = amount,
                TaxAmount = tax,
                Notes = notes,
                FromUser = fromUser,
                ToUser = toUser
            };
            _context.CoinTransactions.Add(coinTransaction);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notificationService.SendCoinTransactionNotificationAsync(toUserId, receivedAmount, TransactionType.Transfer, fromUser.UserName);

            return (true, "transfer.success");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred during coin transfer from {FromUserId} to {ToUserId}.", fromUserId, toUserId);
            return (false, "error.transfer.unknown");
        }
    }

    public async Task<(bool Success, string Message)> TipCoinsAsync(int fromUserId, int toUserId, ulong amount, string? notes)
    {
        if (fromUserId == toUserId) return (false, "error.tip.self");
        if (amount == 0) return (false, "error.tip.zeroAmount");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fromUser = await _context.Users.FindAsync(fromUserId);
            var toUser = await _context.Users.FindAsync(toUserId);

            if (fromUser == null || toUser == null) return (false, "error.user.notFound");
            if (fromUser.Coins < amount) return (false, "error.tip.insufficientCoins");

            var settings = await _settingsService.GetSiteSettingsAsync();
            var tax = (ulong)(amount * settings.TipTaxRate);
            var receivedAmount = amount - tax;

            fromUser.Coins -= amount;
            toUser.Coins += receivedAmount;

            var coinTransaction = new CoinTransaction
            {
                Type = TransactionType.Tip,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Amount = amount,
                TaxAmount = tax,
                Notes = notes,
                FromUser = fromUser,
                ToUser = toUser
            };
            _context.CoinTransactions.Add(coinTransaction);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notificationService.SendCoinTransactionNotificationAsync(toUserId, receivedAmount, TransactionType.Tip, fromUser.UserName);

            return (true, "tip.success");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred during coin tip from {FromUserId} to {ToUserId}.", fromUserId, toUserId);
            return (false, "error.tip.unknown");
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<int> GetUnreadMessagesCountAsync(int userId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == userId && !m.IsRead && !m.ReceiverDeleted)
            .CountAsync();
    }

    public async Task<UserPublicProfileDto?> GetUserPublicProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        string? invitedBy = null;
        if (user.InviteId.HasValue)
        {
            var invite = await _context.Invites.Include(i => i.GeneratorUser).FirstOrDefaultAsync(i => i.Id == user.InviteId.Value);
            invitedBy = invite?.GeneratorUser?.UserName;
        }

        var peerStats = await _context.Peers
            .Where(p => p.UserId == userId)
            .Include(p => p.Torrent)
            .GroupBy(p => p.IsSeeder)
            .Select(g => new { IsSeeder = g.Key, Count = g.Count(), Size = g.Sum(p => (long)p.Torrent.Size) })
            .ToListAsync();

        var seedingCount = peerStats.FirstOrDefault(s => s.IsSeeder)?.Count ?? 0;
        var leechingCount = peerStats.FirstOrDefault(s => !s.IsSeeder)?.Count ?? 0;
        var seedingSize = (ulong)(peerStats.FirstOrDefault(s => s.IsSeeder)?.Size ?? 0L);

        return new UserPublicProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Avatar = user.Avatar,
            Signature = user.Signature,
            UploadedBytes = user.UploadedBytes,
            DownloadedBytes = user.DownloadedBytes,
            NominalUploadedBytes = user.NominalUploadedBytes,
            NominalDownloadedBytes = user.NominalDownloadedBytes,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            Coins = user.Coins,
            IsDoubleUploadActive = user.IsDoubleUploadActive,
            DoubleUploadExpiresAt = user.DoubleUploadExpiresAt,
            IsNoHRActive = user.IsNoHRActive,
            NoHRExpiresAt = user.NoHRExpiresAt,
            TotalSeedingTimeMinutes = user.TotalSeedingTimeMinutes,
            TotalLeechingTimeMinutes = user.TotalLeechingTimeMinutes,
            InviteNum = user.InviteNum,
            InvitedBy = invitedBy,
            SeedingSize = seedingSize,
            CurrentSeedingCount = seedingCount,
            CurrentLeechingCount = leechingCount,
        };
    }

    public async Task<List<BadgeDto>> GetUserBadgesAsync(int userId)
    {
        var cacheKey = $"UserBadges:{userId}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<List<BadgeDto>>(cachedData) ?? new List<BadgeDto>();
        }

        var badges = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.Badge!)
            .Select(b => new BadgeDto { Id = b.Id, Code = b.Code })
            .ToListAsync();
            
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(badges), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
        return badges;
    }

    public async Task<User> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found.");
        Mapper.MapTo(profileDto, user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
        {
            throw new Exception("Invalid current password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        await _context.SaveChangesAsync();
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
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found.");
        Mapper.MapTo(updateUserAdminDto, user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Invite>> GetUserInvitesAsync(int userId)
    {
        return await _context.Invites
            .Where(i => i.GeneratorUserId == userId)
            .Include(i => i.GeneratorUser)
            .Include(i => i.UsedByUser)
            .ToListAsync();
    }

    public async Task<Invite> GenerateInviteAsync(int userId, bool chargeForInvite = true)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found.");
        var settings = await _settingsService.GetSiteSettingsAsync();

        if (chargeForInvite)
        {
            if (user.Coins < settings.InvitePrice)
            {
                throw new Exception("Insufficient Coins to generate an invite.");
            }
            user.Coins -= settings.InvitePrice;
        }

        var newInvite = new Invite
        {
            Code = Guid.NewGuid().ToString("N").Substring(0, 16),
            GeneratorUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(settings.InviteExpirationDays),
            GeneratorUser = user
        };

        _context.Invites.Add(newInvite);
        await _context.SaveChangesAsync();
        return newInvite;
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
            UserAgent = "N/A",
            IpAddress = p.IpAddress.ToString(),
            Port = p.Port,
            Uploaded = 0,
            Downloaded = 0,
            IsSeeder = p.IsSeeder,
            LastAnnounceAt = p.LastAnnounce
        });
    }

    public async Task SendLoginVerificationEmailAsync(string userName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null) { return; }
        
        var code = _random.Next(100000, 999999).ToString("D6");
        var cacheKey = $"VerificationCode:Login:{user.Id}";
        await _cache.SetStringAsync(cacheKey, code, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        
        await _emailService.SendVerificationCodeAsync(user, code, "Login Verification", 5);
    }

    public async Task<(string ManualEntryKey, string QrCodeImageUrl)> GenerateTwoFactorSetupAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId) ?? throw new Exception("User not found");
        var tfa = new TwoFactorAuthenticator();
        var secret = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
        
        var cacheKey = $"2FASetup:{userId}";
        await _cache.SetStringAsync(cacheKey, secret, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        var setupInfo = tfa.GenerateSetupCode(_configuration["Jwt:Issuer"] ?? "TorrentHub", user.Email, secret, false);
        return (setupInfo.ManualEntryKey, setupInfo.QrCodeSetupImageUrl);
    }
    
    public async Task<bool> SwitchToAuthenticatorAppAsync(int userId, string code)
    {
        var user = await GetUserByIdAsync(userId) ?? throw new Exception("User not found");
        var cacheKey = $"2FASetup:{userId}";
        var secret = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(secret)) { return false; }

        var tfa = new TwoFactorAuthenticator();
        if (!tfa.ValidateTwoFactorPIN(secret, code)) { return false; }

        user.TwoFactorSecretKey = _dataProtector.Protect(secret);
        user.TwoFactorType = TwoFactorType.AuthenticatorApp;
        await UpdateUserAsync(user);

        await _cache.RemoveAsync(cacheKey);
        return true;
    }

    public async Task<bool> SwitchToEmailAsync(int userId, string code)
    {
        var user = await GetUserByIdAsync(userId) ?? throw new Exception("User not found");

        if (user.TwoFactorType != TwoFactorType.AuthenticatorApp) { return false; }

        if (!await ValidateTwoFactorCodeAsync(user, code)) { return false; }

        user.TwoFactorSecretKey = null;
        user.TwoFactorType = TwoFactorType.Email;
        await UpdateUserAsync(user);

        return true;
    }

    private async Task<bool> ValidateEmailLoginCodeAsync(int userId, string code)
    {
        var cacheKey = $"VerificationCode:Login:{userId}";
        var cachedCode = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedCode) || cachedCode != code) { return false; }
        await _cache.RemoveAsync(cacheKey);
        return true;
    }
    
    private Task<bool> ValidateTwoFactorCodeAsync(User user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey)) { return Task.FromResult(false); }
        try
        {
            var secret = _dataProtector.Unprotect(user.TwoFactorSecretKey);
            var tfa = new TwoFactorAuthenticator();
            return Task.FromResult(tfa.ValidateTwoFactorPIN(secret, code));
        }
        catch (CryptographicException) { return Task.FromResult(false); }
    }

    public async Task EquipBadgeAsync(int userId, int badgeId)
    {
        var user = await GetUserByIdAsync(userId) ?? throw new Exception("User not found.");

        var userHasBadge = await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
        if (!userHasBadge)
        {
            throw new Exception("User does not own this badge.");
        }

        user.EquippedBadgeId = badgeId;
        await UpdateUserAsync(user);
        _logger.LogInformation("User {UserId} equipped badge {BadgeId}.", userId, badgeId);
    }

    public async Task UpdateUserTitleAsync(int userId, string newTitle)
    {
        var user = await GetUserByIdAsync(userId) ?? throw new Exception("User not found.");
        
        // In a real application, we might want to check if the user has purchased the right to change the title.
        // For now, we assume the API endpoint is protected and this check happens at the controller/API level before calling the service.
        if (newTitle.Length > 30)
        {
            throw new ArgumentException("Title cannot be longer than 30 characters.");
        }

        user.UserTitle = newTitle;
        await UpdateUserAsync(user);
        _logger.LogInformation("User {UserId} updated their title.", userId);
    }

    public async Task<bool> ResendVerificationEmailAsync(string userNameOrEmail)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);
        if (user == null)
        {
            // To prevent user enumeration, we don't reveal if the user exists.
            _logger.LogInformation("Resend verification requested for non-existent user or email: {UserNameOrEmail}", userNameOrEmail);
            return true;
        }

        if (user.IsEmailVerified)
        {
            return false;
        }

        var token = await GenerateEmailVerificationToken(user.Id);
        await _emailService.SendEmailVerificationLinkAsync(user, token);

        _logger.LogInformation("Resent email verification for user {UserId}", user.Id);
        return true;
    }
}
