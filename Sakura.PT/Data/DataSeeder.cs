using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sakura.PT.Entities;
using Sakura.PT.Enums;
using Bogus; // Added Bogus

namespace Sakura.PT.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAllDataAsync(ApplicationDbContext context, ILogger logger)
        {
            await context.Database.MigrateAsync();

            // Seed Admin User first, as other entities depend on it
            User adminUser = await SeedDefaultAdminUserAsync(context, logger);

            // Seed other users
            List<User> users = await SeedUsersAsync(context, logger, adminUser, 10); // Seed 10 additional users
            users.Insert(0, adminUser); // Add admin to the list of all users

            // Seed Invites
            await SeedInvitesAsync(context, logger, users, 20); // Seed 20 invites

            // Seed Torrents
            List<Torrent> torrents = await SeedTorrentsAsync(context, logger, users, 50); // Seed 50 torrents

            // Seed Comments
            await SeedCommentsAsync(context, logger, users, torrents, 100); // Seed 100 comments

            // Seed Messages
            await SeedMessagesAsync(context, logger, users, 50); // Seed 50 messages

            // Seed Announcements
            await SeedAnnouncementsAsync(context, logger, users.Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Moderator).ToList(), 5); // Seed 5 announcements

            // Seed Badges and StoreItems
            List<Badge> badges = await SeedBadgesAsync(context, logger, 10); // Seed 10 badges
            await SeedStoreItemsAsync(context, logger, badges, 15); // Seed 15 store items

            // Seed Requests
            await SeedRequestsAsync(context, logger, users, 20); // Seed 20 requests

            // Seed Reports
            await SeedReportsAsync(context, logger, users, torrents, 30); // Seed 30 reports

            // Seed Peers
            await SeedPeersAsync(context, logger, users, torrents, 100); // Seed 100 peers

            // Seed UserBadges
            await SeedUserBadgesAsync(context, logger, users, badges, 30); // Assign 30 user badges

            // Seed UserDailyStats
            await SeedUserDailyStatsAsync(context, logger, users, 50); // Seed 50 daily stats entries

            logger.LogInformation("All mock data seeding completed.");
        }

        public static async Task<User> SeedDefaultAdminUserAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!await context.Users.AnyAsync(u => u.UserName == "Admin"))
            {
                var adminUser = new User
                {
                    UserName = "Admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123"),
                    Role = UserRole.Administrator,
                    CreatedAt = DateTime.UtcNow,
                    Passkey = Guid.NewGuid().ToString("N")
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Default Admin user created successfully.");
                return adminUser;
            }
            else
            {
                logger.LogInformation("Default Admin user already exists.");
                return await context.Users.FirstAsync(u => u.UserName == "Admin");
            }
        }

        public static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context, ILogger logger, User adminUser, int count)
        {
            if (await context.Users.CountAsync() > 1) // Check if more than just admin exists
            {
                logger.LogInformation("Users already exist, skipping seeding.");
                return await context.Users.Where(u => u.Id != adminUser.Id).ToListAsync();
            }

            var userFaker = new Faker<User>()
                .RuleFor(u => u.UserName, f => f.Internet.UserName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.UserName))
                .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword(f.Internet.Password()))
                .RuleFor(u => u.Avatar, f => f.Internet.Avatar())
                .RuleFor(u => u.Signature, f => f.Lorem.Sentence())
                .RuleFor(u => u.Language, f => f.PickRandom("en-US", "zh-CN"))
                .RuleFor(u => u.UploadedBytes, f => (ulong)f.Random.Long(0, 100_000_000_000)) // Up to 100 GB
                .RuleFor(u => u.DownloadedBytes, (f, u) => (ulong)f.Random.Long(0, (long)u.UploadedBytes)) // Downloaded less than uploaded
                .RuleFor(u => u.RssKey, f => f.Random.Guid().ToString("N").Substring(0, 32))
                .RuleFor(u => u.Passkey, f => f.Random.Guid().ToString("N").Substring(0, 32))
                .RuleFor(u => u.Role, f => f.PickRandom<UserRole>(UserRole.User, UserRole.Moderator))
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(5))
                .RuleFor(u => u.IsBanned, f => f.Random.Bool(0.1f)) // 10% chance of being banned
                .RuleFor(u => u.BanReason, (f, u) => u.IsBanned ? f.PickRandom<UserBanReason>() : (UserBanReason?)null)
                .RuleFor(u => u.BanUntil, (f, u) => u.IsBanned ? f.Date.Future(1) : (DateTime?)null)
                .RuleFor(u => u.InviteNum, f => f.Random.UInt(0, 5))
                .RuleFor(u => u.SakuraCoins, f => (ulong)f.Random.Long(0, 1000))
                .RuleFor(u => u.TotalSeedingTimeMinutes, f => (ulong)f.Random.Long(0, 10000))
                .RuleFor(u => u.IsDoubleUploadActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.DoubleUploadExpiresAt, (f, u) => u.IsDoubleUploadActive ? f.Date.Future(1) : (DateTime?)null)
                .RuleFor(u => u.IsNoHRActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.NoHRExpiresAt, (f, u) => u.IsNoHRActive ? f.Date.Future(1) : (DateTime?)null);

            var users = userFaker.Generate(count);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            logger.LogInformation("{Count} users seeded successfully.", count);
            return users;
        }

        public static async Task SeedInvitesAsync(ApplicationDbContext context, ILogger logger, List<User> users, int count)
        {
            if (!await context.Invites.AnyAsync())
            {
                var inviteFaker = new Faker<Invite>()
                    .RuleFor(i => i.Id, f => Guid.NewGuid())
                    .RuleFor(i => i.Code, (f, i) => i.Id.ToString("N").Substring(0, 10))
                    .RuleFor(i => i.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(i => i.ExpiresAt, (f, i) => i.CreatedAt.AddDays(f.Random.Int(1, 30)))
                    .RuleFor(i => i.GeneratorUser, f => f.PickRandom(users));

                var invites = inviteFaker.Generate(count);
                context.Invites.AddRange(invites);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} invite codes seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Invite codes already exist, skipping seeding.");
            }
        }

        public static async Task<List<Torrent>> SeedTorrentsAsync(ApplicationDbContext context, ILogger logger, List<User> users, int count)
        {
            if (!await context.Torrents.AnyAsync())
            {
                var torrentFaker = new Faker<Torrent>()
                    .RuleFor(t => t.Name, f => f.Commerce.ProductName())
                    .RuleFor(t => t.InfoHash, f => f.Random.Guid().ToString("N").Substring(0, 40))
                    .RuleFor(t => t.FilePath, (f, t) => $"/torrents/{t.InfoHash}.torrent")
                    .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
                    .RuleFor(t => t.UploadedByUser, f => f.PickRandom(users))
                    .RuleFor(t => t.Category, f => f.PickRandom<TorrentCategory>())
                    .RuleFor(t => t.Size, f => f.Random.Long(10_000_000, 5_000_000_000)) // 10MB to 5GB
                    .RuleFor(t => t.IsDeleted, f => f.Random.Bool(0.02f)) // 2% chance of being deleted
                    .RuleFor(t => t.CreatedAt, f => f.Date.Past(2))
                    .RuleFor(t => t.IsFree, f => f.Random.Bool(0.1f)) // 10% chance of being free
                    .RuleFor(t => t.FreeUntil, (f, t) => t.IsFree ? f.Date.Future(1) : (DateTime?)null)
                    .RuleFor(t => t.StickyStatus, f => f.PickRandom<TorrentStickyStatus>())
                    .RuleFor(t => t.ImdbId, f => f.Random.Bool(0.5f) ? $"tt{f.Random.Number(1000000, 9999999)}" : null);

                var torrents = torrentFaker.Generate(count);
                context.Torrents.AddRange(torrents);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} torrents seeded successfully.", count);
                return torrents;
            }
            else
            {
                logger.LogInformation("Torrents already exist, skipping seeding.");
                return await context.Torrents.ToListAsync();
            }
        }

        public static async Task SeedCommentsAsync(ApplicationDbContext context, ILogger logger, List<User> users, List<Torrent> torrents, int count)
        {
            if (!await context.Comments.AnyAsync() && torrents.Any() && users.Any())
            {
                var commentFaker = new Faker<Comment>()
                    .RuleFor(c => c.Text, f => f.Lorem.Sentence())
                    .RuleFor(c => c.Torrent, f => f.PickRandom(torrents))
                    .RuleFor(c => c.User, f => f.PickRandom(users))
                    .RuleFor(c => c.CreatedAt, f => f.Date.Past(1));

                var comments = commentFaker.Generate(count);
                context.Comments.AddRange(comments);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} comments seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Comments already exist or no torrents/users to comment on, skipping seeding.");
            }
        }

        public static async Task SeedMessagesAsync(ApplicationDbContext context, ILogger logger, List<User> users, int count)
        {
            if (!await context.Messages.AnyAsync() && users.Count >= 2)
            {
                var messageFaker = new Faker<Message>()
                    .RuleFor(m => m.Sender, f => f.PickRandom(users))
                    .RuleFor(m => m.Receiver, (f, m) => f.PickRandom(users.Where(u => u.Id != m.Sender!.Id).ToList())) // Ensure sender != receiver
                    .RuleFor(m => m.Subject, f => f.Lorem.Sentence(5))
                    .RuleFor(m => m.Content, f => f.Lorem.Paragraph())
                    .RuleFor(m => m.SentAt, f => f.Date.Past(1))
                    .RuleFor(m => m.IsRead, f => f.Random.Bool(0.7f)) // 70% chance of being read
                    .RuleFor(m => m.SenderDeleted, f => f.Random.Bool(0.1f))
                    .RuleFor(m => m.ReceiverDeleted, f => f.Random.Bool(0.1f));

                var messages = messageFaker.Generate(count);
                context.Messages.AddRange(messages);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} messages seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Messages already exist or not enough users to send messages, skipping seeding.");
            }
        }

        public static async Task SeedAnnouncementsAsync(ApplicationDbContext context, ILogger logger, List<User> adminModerators, int count)
        {
            if (!await context.Announcements.AnyAsync() && adminModerators.Any())
            {
                var announcementFaker = new Faker<Announcement>()
                    .RuleFor(a => a.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(a => a.Content, f => f.Lorem.Paragraph())
                    .RuleFor(a => a.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(a => a.CreatedByUser, f => f.PickRandom(adminModerators));

                var announcements = announcementFaker.Generate(count);
                context.Announcements.AddRange(announcements);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} announcements seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Announcements already exist or no admin/moderator users, skipping seeding.");
            }
        }

        public static async Task<List<Badge>> SeedBadgesAsync(ApplicationDbContext context, ILogger logger, int count)
        {
            if (!await context.Badges.AnyAsync())
            {
                var badgeFaker = new Faker<Badge>()
                    .RuleFor(b => b.Name, f => f.Commerce.ProductAdjective() + " Badge")
                    .RuleFor(b => b.Description, f => f.Lorem.Sentence())
                    .RuleFor(b => b.ImageUrl, f => f.Image.DataUri(100, 100)) // Placeholder image
                    .RuleFor(b => b.IsPurchasable, f => f.Random.Bool(0.5f)); // 50% chance of being purchasable

                var badges = badgeFaker.Generate(count);
                context.Badges.AddRange(badges);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} badges seeded successfully.", count);
                return badges;
            }
            else
            {
                logger.LogInformation("Badges already exist, skipping seeding.");
                return await context.Badges.ToListAsync();
            }
        }

        public static async Task SeedStoreItemsAsync(ApplicationDbContext context, ILogger logger, List<Badge> badges, int count)
        {
            if (!await context.StoreItems.AnyAsync())
            {
                var storeItemFaker = new Faker<StoreItem>()
                    .RuleFor(si => si.ItemCode, f => f.PickRandom<StoreItemCode>())
                    .RuleFor(si => si.Name, (f, si) => si.ItemCode.ToString() + " Item")
                    .RuleFor(si => si.Description, f => f.Lorem.Sentence())
                    .RuleFor(si => si.Price, f => (ulong)f.Random.Long(10, 1000))
                    .RuleFor(si => si.IsAvailable, f => f.Random.Bool(0.8f)) // 80% chance of being available
                    .RuleFor(si => si.Badge, (f, si) => si.ItemCode == StoreItemCode.Badge && badges.Any() ? f.PickRandom(badges) : null);

                var storeItems = storeItemFaker.Generate(count);
                context.StoreItems.AddRange(storeItems);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} store items seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Store items already exist, skipping seeding.");
            }
        }

        public static async Task SeedRequestsAsync(ApplicationDbContext context, ILogger logger, List<User> users, int count)
        {
            if (!await context.Requests.AnyAsync() && users.Any())
            {
                var requestFaker = new Faker<Request>()
                    .RuleFor(r => r.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(r => r.Description, f => f.Lorem.Paragraph())
                    .RuleFor(r => r.RequestedByUser, f => f.PickRandom(users))
                    .RuleFor(r => r.Status, f => f.PickRandom<RequestStatus>())
                    .RuleFor(r => r.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(r => r.FilledAt, (f, r) => r.Status == RequestStatus.Filled ? f.Date.Between(r.CreatedAt, DateTime.UtcNow) : (DateTime?)null)
                    .RuleFor(r => r.BountyAmount, f => (ulong)f.Random.Long(100, 5000))
                    .RuleFor(r => r.FilledByUser, (f, r) => r.Status == RequestStatus.Filled ? f.PickRandom(users.Where(u => u.Id != r.RequestedByUser!.Id).ToList()) : null)
                    .RuleFor(r => r.FilledWithTorrent, (f, r) => r.Status == RequestStatus.Filled && context.Torrents.Any() ? f.PickRandom(context.Torrents.ToList()) : null); // Requires torrents to be seeded first

                var requests = requestFaker.Generate(count);
                context.Requests.AddRange(requests);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} requests seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Requests already exist or no users, skipping seeding.");
            }
        }

        public static async Task SeedReportsAsync(ApplicationDbContext context, ILogger logger, List<User> users, List<Torrent> torrents, int count)
        {
            if (!await context.Reports.AnyAsync() && users.Any() && torrents.Any())
            {
                var reportFaker = new Faker<Report>()
                    .RuleFor(r => r.Torrent, f => f.PickRandom(torrents))
                    .RuleFor(r => r.ReporterUser, f => f.PickRandom(users))
                    .RuleFor(r => r.Reason, f => f.PickRandom<ReportReason>())
                    .RuleFor(r => r.Details, f => f.Lorem.Sentence())
                    .RuleFor(r => r.ReportedAt, f => f.Date.Past(1))
                    .RuleFor(r => r.IsProcessed, f => f.Random.Bool(0.5f)) // 50% chance of being processed
                    .RuleFor(r => r.ProcessedByUser, (f, r) => r.IsProcessed ? f.PickRandom(users.Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Moderator).ToList()) : null)
                    .RuleFor(r => r.ProcessedAt, (f, r) => r.IsProcessed ? f.Date.Between(r.ReportedAt, DateTime.UtcNow) : (DateTime?)null)
                    .RuleFor(r => r.AdminNotes, (f, r) => r.IsProcessed ? f.Lorem.Sentence() : null);

                var reports = reportFaker.Generate(count);
                context.Reports.AddRange(reports);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} reports seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Reports already exist or no users/torrents, skipping seeding.");
            }
        }

        public static async Task SeedPeersAsync(ApplicationDbContext context, ILogger logger, List<User> users, List<Torrent> torrents, int count)
        {
            if (!await context.Peers.AnyAsync() && users.Any() && torrents.Any())
            {
                var peerFaker = new Faker<Peers>()
                    .RuleFor(p => p.Torrent, f => f.PickRandom(torrents))
                    .RuleFor(p => p.User, f => f.PickRandom(users))
                    .RuleFor(p => p.IpAddress, f => f.Internet.Ip())
                    .RuleFor(p => p.Port, f => f.Internet.Port())
                    .RuleFor(p => p.LastAnnounce, f => f.Date.Recent(1))
                    .RuleFor(p => p.IsSeeder, f => f.Random.Bool());

                var peers = peerFaker.Generate(count);
                context.Peers.AddRange(peers);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} peers seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("Peers already exist or no users/torrents, skipping seeding.");
            }
        }

        public static async Task SeedUserBadgesAsync(ApplicationDbContext context, ILogger logger, List<User> users, List<Badge> badges, int count)
        {
            if (!await context.UserBadges.AnyAsync() && users.Any() && badges.Any())
            {
                var userBadgeFaker = new Faker<UserBadge>()
                    .RuleFor(ub => ub.User, f => f.PickRandom(users))
                    .RuleFor(ub => ub.Badge, f => f.PickRandom(badges))
                    .RuleFor(ub => ub.AcquiredAt, f => f.Date.Past(1));

                var userBadges = userBadgeFaker.Generate(count);
                context.UserBadges.AddRange(userBadges);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} user badges seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("User badges already exist or no users/badges, skipping seeding.");
            }
        }

        public static async Task SeedUserDailyStatsAsync(ApplicationDbContext context, ILogger logger, List<User> users, int count)
        {
            if (!await context.UserDailyStats.AnyAsync() && users.Any())
            {
                var userDailyStatsFaker = new Faker<UserDailyStats>()
                    .RuleFor(uds => uds.User, f => f.PickRandom(users))
                    .RuleFor(uds => uds.Date, f => f.Date.Past(30).Date) // Last 30 days
                    .RuleFor(uds => uds.CommentBonusesGiven, f => f.Random.Int(0, 5));

                var userDailyStats = userDailyStatsFaker.Generate(count);
                context.UserDailyStats.AddRange(userDailyStats);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} user daily stats seeded successfully.", count);
            }
            else
            {
                logger.LogInformation("User daily stats already exist or no users, skipping seeding.");
            }
        }
    }
}
