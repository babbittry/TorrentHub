using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.Data;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using Bogus;
using TorrentHub.Services.Interfaces;
using System.IO; // Added for Path and Directory operations
using Microsoft.AspNetCore.Hosting; // Added for IWebHostEnvironment

namespace TorrentHub.Data
{
    public static class DataSeeder
    {
        public static async Task SeedFoundationalDataAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!await context.Badges.AnyAsync())
            {
                var badges = new List<Badge>
                {
                    new Badge { Id = 1, Code = BadgeCode.EarlySupporter },
                    new Badge { Id = 2, Code = BadgeCode.TorrentMaster },
                    new Badge { Id = 3, Code = BadgeCode.CommunityContributor },
                    new Badge { Id = 4, Code = BadgeCode.CoinCollector }
                };
                context.Badges.AddRange(badges);
                logger.LogInformation("Seeded default badges.");
            }

            if (!await context.ForumCategories.AnyAsync())
            {
                var categories = new List<ForumCategory>
                {
                    new ForumCategory { Id = 1, Code = ForumCategoryCode.Announcement, DisplayOrder = 1 },
                    new ForumCategory { Id = 2, Code = ForumCategoryCode.General, DisplayOrder = 2 },
                    new ForumCategory { Id = 3, Code = ForumCategoryCode.Feedback, DisplayOrder = 3 },
                    new ForumCategory { Id = 4, Code = ForumCategoryCode.Invite, DisplayOrder = 4 },
                    new ForumCategory { Id = 5, Code = ForumCategoryCode.Watering, DisplayOrder = 5 }
                };
                context.ForumCategories.AddRange(categories);
                logger.LogInformation("Seeded default forum categories.");
            }

            if (!await context.StoreItems.AnyAsync())
            {
                var storeItems = new List<StoreItem>
                {
                    new StoreItem
                    {
                        Id = 1, ItemCode = StoreItemCode.UploadCredit10GB, Name = "10 GB Upload Credit", Description = "Adds 10 GB to your upload total.", Price = 1000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "10 GB Upload Credit", Description = "Adds 10 GB to your upload total." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "10 GB 上传流量", Description = "为您的上传总量增加 10 GB。" },
                            new StoreItemTranslation { Language = "fr", Name = "Crédit d'upload de 10 Go", Description = "Ajoute 10 Go à votre total d'upload." },
                            new StoreItemTranslation { Language = "ja", Name = "10 GB アップロードクレジット", Description = "アップロード合計に 10 GB を追加します。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 2, ItemCode = StoreItemCode.UploadCredit50GB, Name = "50 GB Upload Credit", Description = "Adds 50 GB to your upload total.", Price = 4500, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "50 GB Upload Credit", Description = "Adds 50 GB to your upload total." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "50 GB 上传流量", Description = "为您的上传总量增加 50 GB。" },
                            new StoreItemTranslation { Language = "fr", Name = "Crédit d'upload de 50 Go", Description = "Ajoute 50 Go à votre total d'upload." },
                            new StoreItemTranslation { Language = "ja", Name = "50 GB アップロードクレジット", Description = "アップロード合計に 50 GB を追加します。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 3, ItemCode = StoreItemCode.InviteOne, Name = "Single Invite", Description = "Grants you one invitation to share.", Price = 5000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "Single Invite", Description = "Grants you one invitation to share." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "一枚邀请码", Description = "赠与您一枚可分享的邀请码。" },
                            new StoreItemTranslation { Language = "fr", Name = "Invitation unique", Description = "Vous accorde une invitation à partager." },
                            new StoreItemTranslation { Language = "ja", Name = "招待コード1枚", Description = "共有できる招待コードを1枚進呈します。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 4, ItemCode = StoreItemCode.InviteFive, Name = "Five Invites", Description = "Grants you five invitations to share.", Price = 20000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "Five Invites", Description = "Grants you five invitations to share." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "五枚邀请码", Description = "赠与您五枚可分享的邀请码。" },
                            new StoreItemTranslation { Language = "fr", Name = "Cinq invitations", Description = "Vous accorde cinq invitations à partager." },
                            new StoreItemTranslation { Language = "ja", Name = "招待コード5枚", Description = "共有できる招待コードを5枚進呈します。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 5, ItemCode = StoreItemCode.DoubleUpload, Name = "24hr Double Upload", Description = "All uploads are counted as double for 24 hours.", Price = 10000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "24hr Double Upload", Description = "All uploads are counted as double for 24 hours." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "24小时双倍上传", Description = "24小时内所有上传均按双倍计算。" },
                            new StoreItemTranslation { Language = "fr", Name = "Upload double 24h", Description = "Tous les uploads sont comptés double pendant 24 heures." },
                            new StoreItemTranslation { Language = "ja", Name = "24時間アップロード2倍", Description = "24時間、すべてのアップロードが2倍としてカウントされます。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 6, ItemCode = StoreItemCode.NoHitAndRun, Name = "72hr H&R Immunity", Description = "You are immune to Hit & Run warnings for 72 hours.", Price = 15000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "72hr H&R Immunity", Description = "You are immune to Hit & Run warnings for 72 hours." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "72小时 H&R 豁免", Description = "您在72小时内免疫 Hit & Run 警告。" },
                            new StoreItemTranslation { Language = "fr", Name = "Immunité H&R 72h", Description = "Vous êtes immunisé contre les avertissements Hit & Run pendant 72 heures." },
                            new StoreItemTranslation { Language = "ja", Name = "72時間ヒットエンドラン免除", Description = "72時間、ヒットエンドラン警告が免除されます。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 7, ItemCode = StoreItemCode.ChangeUsername, Name = "Username Change Card", Description = "Allows you to change your username once.", Price = 30000, IsAvailable = true,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "Username Change Card", Description = "Allows you to change your username once." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "改名卡", Description = "允许您更改一次您的用户名。" },
                            new StoreItemTranslation { Language = "fr", Name = "Carte de changement de nom", Description = "Vous permet de changer votre nom d'utilisateur une seule fois." },
                            new StoreItemTranslation { Language = "ja", Name = "ユーザー名変更カード", Description = "ユーザー名を一度だけ変更できます。" }
                        }
                    },
                    new StoreItem
                    {
                        Id = 8, ItemCode = StoreItemCode.Badge, Name = "Coin Collector Badge", Description = "Purchase the exclusive Coin Collector badge.", Price = 25000, IsAvailable = true, BadgeId = 4,
                        Translations = new List<StoreItemTranslation>
                        {
                            new StoreItemTranslation { Language = "en", Name = "Coin Collector Badge", Description = "Purchase the exclusive Coin Collector badge." },
                            new StoreItemTranslation { Language = "zh-CN", Name = "金币收藏家徽章", Description = "购买专属的金币收藏家徽章。" },
                            new StoreItemTranslation { Language = "fr", Name = "Badge Collectionneur de pièces", Description = "Achetez le badge exclusif de collectionneur de pièces." },
                            new StoreItemTranslation { Language = "ja", Name = "コインコレクターバッジ", Description = "限定のコインコレクターバッジを購入します。" }
                        }
                    }
                };
                context.StoreItems.AddRange(storeItems);
                logger.LogInformation("Seeded default store items.");
            }
            
            if (!await context.SiteSettings.AnyAsync(s => s.Key == "IsRegistrationOpen"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "IsRegistrationOpen", Value = "false" });
                logger.LogInformation("Default site settings seeded successfully.");
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedMockDataAsync(ApplicationDbContext context, ILogger logger,
            ITMDbService tmdbService, IWebHostEnvironment env)
        {
            await context.Database.MigrateAsync();

            // Seed Admin User first, as other entities depend on it
            User adminUser = await SeedDefaultAdminUserAsync(context, logger);

            // Seed other users
            List<User> users = await SeedUsersAsync(context, logger, adminUser, 10, env); // Pass env
            users.Insert(0, adminUser); // Add admin to the list of all users

            // Seed Invites
            await SeedInvitesAsync(context, logger, users, 20); // Seed 20 invites

            // Seed Torrents
            List<Torrent> torrents = await SeedTorrentsAsync(context, logger, users, tmdbService);

            // Seed Comments
            await SeedCommentsAsync(context, logger, users, torrents, 100); // Seed 100 comments

            // Seed Messages
            await SeedMessagesAsync(context, logger, users, 50); // Seed 50 messages

            // Seed Announcements
            await SeedAnnouncementsAsync(context, logger,
                users.Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Moderator).ToList(),
                5); // Seed 5 announcements

            var badges = await context.Badges.ToListAsync();

            // Seed Requests
            await SeedRequestsAsync(context, logger, users, torrents, 20); // Seed 20 requests

            // Seed Reports
            await SeedReportsAsync(context, logger, users, torrents, 30); // Seed 30 reports

            // Seed Peers
            await SeedPeersAsync(context, logger, users, torrents, 100); // Seed 100 peers

            // Seed UserBadges
            await SeedUserBadgesAsync(context, logger, users, badges, 30); // Assign 30 user badges

            // Seed UserDailyStats
            await SeedUserDailyStatsAsync(context, logger, users, 50); // Seed 50 daily stats entries

            // Seed Forum Data
            await SeedForumDataAsync(context, logger, users);

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
                    CreatedAt = DateTimeOffset.UtcNow,
                    Passkey = Guid.NewGuid(),
                    RssKey = Guid.NewGuid()
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

        public static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context, ILogger logger,
            User adminUser, int count, IWebHostEnvironment env)
        {
            if (await context.Users.CountAsync() > 1) // Check if more than just admin exists
            {
                logger.LogInformation("Users already exist, skipping seeding.");
                return await context.Users.Where(u => u.Id != adminUser.Id).ToListAsync();
            }

            var userFaker = new Faker<User>()
                .RuleFor(u => u.UserName, f =>
                {
                    var name = f.Internet.UserName(f.Name.FirstName(), f.Name.LastName());
                    var namePart = name.Substring(0, Math.Min(name.Length, 9));
                    var guidPart = Guid.NewGuid().ToString("N").Substring(0, 10);
                    return $"{namePart}_{guidPart}";
                })
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.UserName))
                .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword(f.Internet.Password()))
                .RuleFor(u => u.Avatar, (f, u) =>
                {
                    var firstLetter = u.UserName.Length > 0 ? u.UserName[0].ToString().ToUpper() : "U";
                    var color = f.Internet.Color().Replace("#", ""); // Get a hex color without #
                    var svgContent = GenerateSimpleAvatarSvg(firstLetter, color);

                    var avatarDirectory = Path.Combine(env.WebRootPath, "avatars");
                    if (!Directory.Exists(avatarDirectory))
                    {
                        Directory.CreateDirectory(avatarDirectory);
                    }

                    var avatarFileName = $"{Guid.NewGuid().ToString("N")}.svg";
                    var fullPath = Path.Combine(avatarDirectory, avatarFileName);
                    File.WriteAllText(fullPath, svgContent); // Use WriteAllText for sync in faker rule

                    return $"/avatars/{avatarFileName}";
                })
                .RuleFor(u => u.Signature, f => f.Lorem.Sentence(5))
                .RuleFor(u => u.Language, f => f.PickRandom("en-US", "zh-CN"))
                .RuleFor(u => u.UploadedBytes, f => (ulong)f.Random.Long(0, 100_000_000_000)) // Up to 100 GB
                .RuleFor(u => u.DownloadedBytes,
                    (f, u) => (ulong)f.Random.Long(0, (long)u.UploadedBytes)) // Downloaded less than uploaded
                .RuleFor(u => u.RssKey, f => f.Random.Guid())
                .RuleFor(u => u.Passkey, f => f.Random.Guid())
                .RuleFor(u => u.Role, f => f.PickRandom<UserRole>(UserRole.User, UserRole.Moderator))
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(5).ToUniversalTime())
                .RuleFor(u => u.BanStatus, f => f.Random.Bool(0.1f) ? f.PickRandom<BanStatus>() : BanStatus.None)
                .RuleFor(u => u.BanReason, (f, u) => u.BanStatus != BanStatus.None ? f.Lorem.Sentence(50).Substring(0, 50) : null)
                .RuleFor(u => u.BanUntil, (f, u) => u.BanStatus != BanStatus.None ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.InviteNum, f => f.Random.UInt(0, 5))
                .RuleFor(u => u.Coins, f => (ulong)f.Random.Long(0, 1000))
                .RuleFor(u => u.TotalSeedingTimeMinutes, f => (ulong)f.Random.Long(0, 10000))
                .RuleFor(u => u.IsDoubleUploadActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.DoubleUploadExpiresAt,
                    (f, u) => u.IsDoubleUploadActive ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.IsNoHRActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.NoHRExpiresAt,
                    (f, u) => u.IsNoHRActive ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.InviteId, f => null); // Explicitly set InviteId to null

            var users = userFaker.Generate(count);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            logger.LogInformation("{Count} users seeded successfully.", count);
            return users;
        }

        public static async Task SeedInvitesAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            int count)
        {
            if (!await context.Invites.AnyAsync())
            {
                var inviteFaker = new Faker<Invite>()
                    .RuleFor(i => i.Id, f => Guid.NewGuid())
                    .RuleFor(i => i.Code, (f, i) => i.Id.ToString("N").Substring(0, 10))
                    .RuleFor(i => i.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
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

        public static async Task<List<Torrent>> SeedTorrentsAsync(ApplicationDbContext context, ILogger logger,
            List<User> users, ITMDbService tmdbService)
        {
            if (await context.Torrents.AnyAsync())
            {
                logger.LogInformation("Torrents already exist, skipping seeding.");
                return await context.Torrents.ToListAsync();
            }

            var movieIds = new List<string>
            {
                "278", // The Shawshank Redemption
                "238", // The Godfather
                "240", // The Godfather Part II
                "155", // The Dark Knight
                "680", // Pulp Fiction
                "13", // Forrest Gump
                "550", // Fight Club
                "122", // The Lord of the Rings: The Return of the King
                "27205", // Inception
                "19995" // Avatar
            };

            var torrents = new List<Torrent>();
            var faker = new Faker();

            foreach (var movieId in movieIds)
            {
                try
                {
                    var movie = await tmdbService.GetMovieByTmdbIdAsync(movieId, "zh-CN");
                    if (movie != null)
                    {
                        var infoHashBytes = faker.Random.Bytes(20);
                        var torrent = new Torrent
                        {
                            Name = movie.Title ?? "unknown",
                            InfoHash = infoHashBytes,
                            FilePath = $"/torrents/{BitConverter.ToString(infoHashBytes).Replace("-", "").ToLowerInvariant()}.torrent",
                            Description = movie.Overview,
                            UploadedByUser = faker.PickRandom(users),
                            Category = TorrentCategory.Movie,
                            Size = faker.Random.Long(10_000_000, 5_000_000_000), // 10MB to 5GB
                            IsDeleted = false,
                            CreatedAt = faker.Date.Past(2).ToUniversalTime(),
                            IsFree = faker.Random.Bool(0.1f),
                            FreeUntil = null,
                            StickyStatus = faker.PickRandom<TorrentStickyStatus>(),

                            // TMDb Fields
                            ImdbId = movie.ImdbId,
                            TMDbId = movie.Id,
                            OriginalTitle = movie.OriginalTitle,
                            Tagline = movie.Tagline,
                            Year = !string.IsNullOrEmpty(movie.ReleaseDate) &&
                                   DateTime.TryParse(movie.ReleaseDate, out var releaseDate)
                                ? releaseDate.Year
                                : null,
                            PosterPath = movie.PosterPath,
                            BackdropPath = movie.BackdropPath,
                            Runtime = movie.Runtime,
                            Genres = movie.Genres.Select(g => g.Name).ToList(),
                            Rating = movie.VoteAverage
                        };

                        if (torrent.IsFree)
                        {
                            torrent.FreeUntil = faker.Date.Future(1).ToUniversalTime();
                        }

                        torrents.Add(torrent);
                        logger.LogInformation("Successfully fetched and created torrent for movie: {MovieTitle}",
                            movie.Title);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch movie data for TMDb ID: {MovieId}", movieId);
                }
            }

            if (torrents.Any())
            {
                context.Torrents.AddRange(torrents);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} torrents seeded successfully from TMDb.", torrents.Count);
            }

            return torrents;
        }


        public static async Task SeedCommentsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Torrent> torrents, int count)
        {
            if (!await context.Comments.AnyAsync() && torrents.Any() && users.Any())
            {
                var commentFaker = new Faker<Comment>()
                    .RuleFor(c => c.Text, f => f.Lorem.Sentence())
                    .RuleFor(c => c.Torrent, f => f.PickRandom(torrents))
                    .RuleFor(c => c.User, f => f.PickRandom(users))
                    .RuleFor(c => c.CreatedAt, f => f.Date.Past(1).ToUniversalTime());

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

        public static async Task SeedMessagesAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            int count)
        {
            if (!await context.Messages.AnyAsync() && users.Count >= 2)
            {
                var messageFaker = new Faker<Message>()
                    .RuleFor(m => m.Sender, f => f.PickRandom(users))
                    .RuleFor(m => m.Receiver,
                        (f, m) => f.PickRandom(users.Where(u => u.Id != m.Sender!.Id)
                            .ToList())) // Ensure sender != receiver
                    .RuleFor(m => m.Subject, f => f.Lorem.Sentence(5))
                    .RuleFor(m => m.Content, f => f.Lorem.Paragraph())
                    .RuleFor(m => m.SentAt, f => f.Date.Past(1).ToUniversalTime())
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

        public static async Task SeedAnnouncementsAsync(ApplicationDbContext context, ILogger logger,
            List<User> adminModerators, int count)
        {
            if (!await context.Announcements.AnyAsync() && adminModerators.Any())
            {
                var announcementFaker = new Faker<Announcement>()
                    .RuleFor(a => a.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(a => a.Content, f => f.Lorem.Paragraph())
                    .RuleFor(a => a.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
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

        public static async Task SeedRequestsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Torrent> torrents, int count)
        {
            if (!await context.Requests.AnyAsync() && users.Any())
            {
                var requestFaker = new Faker<Request>()
                    .RuleFor(r => r.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(r => r.Description, f => f.Lorem.Paragraph())
                    .RuleFor(r => r.RequestedByUser, f => f.PickRandom(users))
                    .RuleFor(r => r.Status, f => f.PickRandom<RequestStatus>())
                    .RuleFor(r => r.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
                    .RuleFor(r => r.FilledAt,
                        (f, r) => r.Status == RequestStatus.Filled
                            ? f.Date.Between(r.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime()
                            : (DateTime?)null)
                    .RuleFor(r => r.BountyAmount, f => (ulong)f.Random.Long(100, 5000))
                    .RuleFor(r => r.FilledByUser,
                        (f, r) => r.Status == RequestStatus.Filled
                            ? f.PickRandom(users.Where(u => u.Id != r.RequestedByUser!.Id).ToList())
                            : null)
                    .RuleFor(r => r.FilledWithTorrent,
                        (f, r) => r.Status == RequestStatus.Filled && torrents.Any()
                            ? f.PickRandom(torrents.ToList())
                            : null);

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

        public static async Task SeedReportsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Torrent> torrents, int count)
        {
            if (!await context.Reports.AnyAsync() && users.Any() && torrents.Any())
            {
                var reportFaker = new Faker<Report>()
                    .RuleFor(r => r.Torrent, f => f.PickRandom(torrents))
                    .RuleFor(r => r.ReporterUser, f => f.PickRandom(users))
                    .RuleFor(r => r.Reason, f => f.PickRandom<ReportReason>())
                    .RuleFor(r => r.Details, f => f.Lorem.Sentence())
                    .RuleFor(r => r.ReportedAt, f => f.Date.Past(1).ToUniversalTime())
                    .RuleFor(r => r.IsProcessed, f => f.Random.Bool(0.5f)) // 50% chance of being processed
                    .RuleFor(r => r.ProcessedByUser,
                        (f, r) => r.IsProcessed
                            ? f.PickRandom(users.Where(u =>
                                u.Role == UserRole.Administrator || u.Role == UserRole.Moderator).ToList())
                            : null)
                    .RuleFor(r => r.ProcessedAt,
                        (f, r) => r.IsProcessed
                            ? f.Date.Between(r.ReportedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime()
                            : (DateTime?)null)
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

        public static async Task SeedPeersAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Torrent> torrents, int count)
        {
            if (!await context.Peers.AnyAsync() && users.Any() && torrents.Any())
            {
                var allPossiblePairs = users.SelectMany(user => torrents.Select(torrent => new { user, torrent }))
                    .ToList();

                var faker = new Faker();
                var uniquePairs = faker.PickRandom(allPossiblePairs, Math.Min(count, allPossiblePairs.Count)).ToList();

                var peers = uniquePairs.Select(pair => new Peers
                {
                    Torrent = pair.torrent,
                    User = pair.user,
                    IpAddress = System.Net.IPAddress.Parse(faker.Internet.Ip()),
                    Port = faker.Internet.Port(),
                    LastAnnounce = faker.Date.Recent(1).ToUniversalTime(),
                    IsSeeder = faker.Random.Bool(),
                    UserAgent = (new Func<string>(() => { var ua = faker.Internet.UserAgent(); return ua.Substring(0, Math.Min(ua.Length, 50)); }))() // Add fake user agent, limited to 50 chars
                }).ToList();

                context.Peers.AddRange(peers);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} peers seeded successfully.", peers.Count);
            }
            else
            {
                logger.LogInformation("Peers already exist or no users/torrents, skipping seeding.");
            }
        }

        public static async Task SeedUserBadgesAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Badge> badges, int count)
        {
            if (!await context.UserBadges.AnyAsync() && users.Any() && badges.Any())
            {
                var allPossiblePairs = users.SelectMany(user => badges.Select(badge => new { user, badge })).ToList();

                var faker = new Faker();
                var uniquePairs = faker.PickRandom(allPossiblePairs, Math.Min(count, allPossiblePairs.Count)).ToList();

                var userBadges = uniquePairs.Select(pair => new UserBadge
                {
                    User = pair.user,
                    Badge = pair.badge,
                    AcquiredAt = faker.Date.Past(1).ToUniversalTime()
                }).ToList();

                context.UserBadges.AddRange(userBadges);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} user badges seeded successfully.", userBadges.Count);
            }
            else
            {
                logger.LogInformation("User badges already exist or no users/badges, skipping seeding.");
            }
        }

        public static async Task SeedUserDailyStatsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            int count)
        {
            if (!await context.UserDailyStats.AnyAsync() && users.Any())
            {
                var faker = new Faker();
                var dates = Enumerable.Range(0, 30).Select(i => faker.Date.Past(i).Date).Distinct().ToList();

                var allPossiblePairs = users.SelectMany(user => dates.Select(date => new { user, date })).ToList();
                var uniquePairs = faker.PickRandom(allPossiblePairs, Math.Min(count, allPossiblePairs.Count)).ToList();

                var userDailyStats = uniquePairs.Select(pair => new UserDailyStats
                {
                    User = pair.user,
                    Date = pair.date.ToUniversalTime(),
                    CommentBonusesGiven = faker.Random.Int(0, 5)
                }).ToList();

                context.UserDailyStats.AddRange(userDailyStats);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} user daily stats seeded successfully.", userDailyStats.Count);
            }
            else
            {
                logger.LogInformation("User daily stats already exist or no users, skipping seeding.");
            }
        }

        public static async Task SeedForumDataAsync(ApplicationDbContext context, ILogger logger, List<User> users)
        {
            // 1. Seed Forum Categories
            if (!await context.ForumCategories.AnyAsync())
            {
                var categories = Enum.GetValues(typeof(ForumCategoryCode))
                    .Cast<ForumCategoryCode>()
                    .Select((code, index) => new ForumCategory
                    {
                        Code = code,
                        DisplayOrder = index + 1
                    })
                    .ToList();

                context.ForumCategories.AddRange(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} forum categories seeded successfully.", categories.Count);
            }
            else
            {
                logger.LogInformation("Forum categories already exist, skipping seeding.");
            }

            var forumCategories = await context.ForumCategories.ToListAsync();

            // 2. Seed Forum Topics and Posts
            if (!await context.ForumTopics.AnyAsync() && users.Any() && forumCategories.Any())
            {
                var topicFaker = new Faker<ForumTopic>()
                    .RuleFor(t => t.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(t => t.Author, f => f.PickRandom(users))
                    .RuleFor(t => t.Category, f => f.PickRandom(forumCategories))
                    .RuleFor(t => t.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
                    .RuleFor(t => t.IsSticky, f => f.Random.Bool(0.1f))
                    .RuleFor(t => t.IsLocked, f => f.Random.Bool(0.05f));

                var topics = topicFaker.Generate(25); // Create 25 topics

                var postFaker = new Faker<ForumPost>()
                    .RuleFor(p => p.Content, f => { var content = string.Join("\n", f.Lorem.Paragraphs(f.Random.Int(1, 4))); return content.Substring(0, Math.Min(content.Length, 1000)); })
                    .RuleFor(p => p.Author, f => f.PickRandom(users));

                var dateFaker = new Faker();

                foreach (var topic in topics)
                {
                    var postCount = new Faker().Random.Int(1, 15);
                    var posts = new List<ForumPost>();

                    // Ensure the first post is by the topic author
                    var firstPost = postFaker.Clone()
                        .RuleFor(p => p.Author, (f, p) => topic.Author)
                        .Generate();

                    firstPost.Topic = topic;
                    firstPost.CreatedAt = topic.CreatedAt;
                    posts.Add(firstPost);

                    // Generate subsequent posts
                    for (int i = 1; i < postCount; i++)
                    {
                        var subsequentPost = postFaker.Generate();
                        subsequentPost.Topic = topic;
                        subsequentPost.CreatedAt =
                            dateFaker.Date.Between(topic.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime();
                        posts.Add(subsequentPost);
                    }

                    topic.Posts = posts;
                    topic.LastPostTime = posts.Max(p => p.CreatedAt);
                }

                context.ForumTopics.AddRange(topics);
                await context.SaveChangesAsync();
                logger.LogInformation("{TopicCount} forum topics and their posts seeded successfully.", topics.Count);
            }
            else
            {
                logger.LogInformation("Forum topics already exist or no users/categories, skipping seeding.");
            }

            logger.LogInformation("All mock data seeding completed.");
        }
    private static string GenerateSimpleAvatarSvg(string letter, string color)
        {
            return $@"<svg width=""100"" height=""100"" viewBox=""0 0 100 100"" xmlns=""http://www.w3.org/2000/svg"">
                <rect width=""100"" height=""100"" fill=""#{color}"" />
                <text x=""50%"" y=""50%"" dominant-baseline=""middle"" text-anchor=""middle"" font-family=""Arial, sans-serif"" font-size=""50"" fill=""white"">{letter}</text>
            </svg>";
        }
    }
}

