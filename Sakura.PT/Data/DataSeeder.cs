using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sakura.PT.Entities;
using Sakura.PT.Enums;

namespace Sakura.PT.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDefaultAdminUserAsync(ApplicationDbContext context, ILogger logger)
        {
            // 确保数据库已迁移到最新版本
            await context.Database.MigrateAsync();

            // 检查 Admin 用户是否已存在
            if (!await context.Users.AnyAsync(u => u.UserName == "Admin"))
            {
                var adminUser = new User
                {
                    UserName = "Admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123"), // **重要：在生产环境中请使用更安全的密码或从配置中读取**
                    Role = UserRole.Administrator,
                    CreatedAt = DateTime.UtcNow,
                    Passkey = Guid.NewGuid().ToString("N") // 生成一个唯一的Passkey
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Default Admin user created successfully.");
            }
            else
            {
                logger.LogInformation("Default Admin user already exists.");
            }
        }

        public static async Task SeedInviteCodesAsync(ApplicationDbContext context, ILogger logger, int count = 5)
        {
            if (!await context.Invites.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
                if (adminUser == null)
                {
                    logger.LogWarning("Admin user not found, cannot seed invite codes.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    var invite = new Invite
                    {
                        Code = Guid.NewGuid().ToString("N").Substring(0, 10), // 生成一个10位数的邀请码
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(7), // 邀请码有效期7天
                        GeneratorUserId = adminUser.Id,
                        GeneratorUser = adminUser // <-- 新增这一行
                    };
                    context.Invites.Add(invite);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} invite codes seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Invite codes already exist, skipping seeding.");
            }
        }

        public static async Task SeedTorrentsAsync(ApplicationDbContext context, ILogger logger, int count = 3)
        {
            if (!await context.Torrents.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
                if (adminUser == null)
                {
                    logger.LogWarning("Admin user not found, skipping torrent seeding.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    var torrent = new Torrent
                    {
                        InfoHash = Guid.NewGuid().ToString("N").Substring(0, 40), // 模拟InfoHash
                        Name = $"Test Torrent {i + 1}",
                        Description = $"This is a test torrent for seeding purposes {i + 1}.",
                        Category = TorrentCategory.Movie, // 示例分类
                        Size = 1024 * 1024 * (i + 1), // 1MB, 2MB, 3MB (使用 Size 属性)
                        FilePath = $"/path/to/test_torrent_{i + 1}.torrent", // 添加 FilePath
                        CreatedAt = DateTime.UtcNow,
                        UploadedByUserId = adminUser.Id,
                        UploadedByUser = adminUser, // <-- 新增这一行
                        IsFree = false,
                        StickyStatus = TorrentStickyStatus.None
                    };
                    context.Torrents.Add(torrent);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} test torrents seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Torrents already exist, skipping seeding.");
            }
        }
    }
}