using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public class UserLevelService : IUserLevelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserLevelService> _logger;

    public UserLevelService(ApplicationDbContext context, ILogger<UserLevelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CheckAndPromoteDemoteUsersAsync()
    {
        _logger.LogInformation("Starting user level check and update.");

        var users = await _context.Users.ToListAsync();

        foreach (var user in users)
        {
            var oldRole = user.Role;
            var newRole = CalculateNewRole(user);

            if (oldRole != newRole)
            {
                user.Role = newRole;
                _logger.LogInformation("User {UserId} ({UserName}) role changed from {OldRole} to {NewRole}.", user.Id, user.UserName, oldRole, newRole);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("User level check and update finished.");
    }

    private UserRole CalculateNewRole(User user)
    {
        // Calculate share ratio
        double shareRatio = 0;
        if (user.DownloadedBytes > 0)
        {
            shareRatio = (double)user.UploadedBytes / user.DownloadedBytes;
        }
        else if (user.UploadedBytes > 0) // Downloaded is 0, but uploaded is > 0
        {
            shareRatio = double.PositiveInfinity; // Effectively infinite ratio
        }

        // Define thresholds for each role
        // These values should ideally come from configuration
        const double MosquitoRatio = 1.0;
        const double PowerUserRatio = 1.0;
        const double EliteUserRatio = 2.0;
        const double CrazyUserRatio = 3.0;
        const double VeteranUserRatio = 4.0;

        const long PowerUserUpload = 10L * 1024 * 1024 * 1024; // 10 GB
        const long EliteUserUpload = 100L * 1024 * 1024 * 1024; // 100 GB
        const long CrazyUserUpload = 500L * 1024 * 1024 * 1024; // 500 GB
        const long VeteranUserUpload = 1000L * 1024 * 1024 * 1024; // 1 TB

        const long PowerUserSeedingTime = 7 * 24 * 60; // 1 week in minutes
        const long EliteUserSeedingTime = 30 * 24 * 60; // 1 month in minutes
        const long CrazyUserSeedingTime = 90 * 24 * 60; // 3 months in minutes
        const long VeteranUserSeedingTime = 180 * 24 * 60; // 6 months in minutes

        // Determine role based on highest achievable tier
        if (user.Role == UserRole.Administrator || user.Role == UserRole.Moderator || user.Role == UserRole.Uploader || user.Role == UserRole.Archivist || user.Role == UserRole.VIP)
        {
            // Staff and VIP roles are not automatically demoted/promoted by this service
            return user.Role;
        }

        if (shareRatio < MosquitoRatio)
        {
            return UserRole.Mosquito;
        }

        if (shareRatio >= VeteranUserRatio && user.UploadedBytes >= VeteranUserUpload && user.TotalSeedingTimeMinutes >= VeteranUserSeedingTime)
        {
            return UserRole.VeteranUser;
        }

        if (shareRatio >= CrazyUserRatio && user.UploadedBytes >= CrazyUserUpload && user.TotalSeedingTimeMinutes >= CrazyUserSeedingTime)
        {
            return UserRole.CrazyUser;
        }

        if (shareRatio >= EliteUserRatio && user.UploadedBytes >= EliteUserUpload && user.TotalSeedingTimeMinutes >= EliteUserSeedingTime)
        {
            return UserRole.EliteUser;
        }

        if (shareRatio >= PowerUserRatio && user.UploadedBytes >= PowerUserUpload && user.TotalSeedingTimeMinutes >= PowerUserSeedingTime)
        {
            return UserRole.PowerUser;
        }

        return UserRole.User; // Default role
    }
}
