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
using TorrentHub.Core.DTOs;
using TorrentHub.Services;

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
                        Id = 1, ItemCode = StoreItemCode.UploadCredit10GB, Price = 1000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 2, ItemCode = StoreItemCode.UploadCredit100GB, Price = 9000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 3, ItemCode = StoreItemCode.InviteOne, Price = 5000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 4, ItemCode = StoreItemCode.InviteFive, Price = 20000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 5, ItemCode = StoreItemCode.DoubleUpload, Price = 10000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 6, ItemCode = StoreItemCode.NoHitAndRun, Price = 15000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 7, ItemCode = StoreItemCode.ChangeUsername, Price = 30000, IsAvailable = true
                    },
                    new StoreItem
                    {
                        Id = 8, ItemCode = StoreItemCode.Badge, Price = 25000, IsAvailable = true, BadgeId = 4
                    },
                    new StoreItem
                    {
                        Id = 9, ItemCode = StoreItemCode.UserTitle, Price = 5000, IsAvailable = true, MaxStringLength = 30
                    },
                    new StoreItem
                    {
                        Id = 10, ItemCode = StoreItemCode.ColorfulUsername, Price = 7500, IsAvailable = true
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

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "CreateRequestCost"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "CreateRequestCost", Value = "1000" });
                logger.LogInformation("Default CreateRequestCost setting seeded successfully.");
            }
            
            if (!await context.SiteSettings.AnyAsync(s => s.Key == "TipTaxRate"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "TipTaxRate", Value = "0.10" });
                logger.LogInformation("Default TipTaxRate setting seeded successfully.");
            }
            
            if (!await context.SiteSettings.AnyAsync(s => s.Key == "TransferTaxRate"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "TransferTaxRate", Value = "0.05" });
                logger.LogInformation("Default TransferTaxRate setting seeded successfully.");
            }

            // Tracker Anti-Cheat Settings
            if (!await context.SiteSettings.AnyAsync(s => s.Key == "MinAnnounceIntervalSeconds"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "MinAnnounceIntervalSeconds", Value = "900" });
                logger.LogInformation("Default MinAnnounceIntervalSeconds setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "EnforcedMinAnnounceIntervalSeconds"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "EnforcedMinAnnounceIntervalSeconds", Value = "180" });
                logger.LogInformation("Default EnforcedMinAnnounceIntervalSeconds setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "EnableMultiLocationDetection"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "EnableMultiLocationDetection", Value = "true" });
                logger.LogInformation("Default EnableMultiLocationDetection setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "MultiLocationDetectionWindowMinutes"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "MultiLocationDetectionWindowMinutes", Value = "5" });
                logger.LogInformation("Default MultiLocationDetectionWindowMinutes setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "LogMultiLocationCheating"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "LogMultiLocationCheating", Value = "true" });
                logger.LogInformation("Default LogMultiLocationCheating setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "AllowIpChange"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "AllowIpChange", Value = "true" });
                logger.LogInformation("Default AllowIpChange setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "MinIpChangeIntervalMinutes"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "MinIpChangeIntervalMinutes", Value = "10" });
                logger.LogInformation("Default MinIpChangeIntervalMinutes setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "MinSpeedCheckIntervalSeconds"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "MinSpeedCheckIntervalSeconds", Value = "5" });
                logger.LogInformation("Default MinSpeedCheckIntervalSeconds setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "EnableDownloadSpeedCheck"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "EnableDownloadSpeedCheck", Value = "true" });
                logger.LogInformation("Default EnableDownloadSpeedCheck setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "CheatWarningAnnounceThreshold"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "CheatWarningAnnounceThreshold", Value = "20" });
                logger.LogInformation("Default CheatWarningAnnounceThreshold setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "AutoBanAfterCheatWarnings"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "AutoBanAfterCheatWarnings", Value = "10" });
                logger.LogInformation("Default AutoBanAfterCheatWarnings setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "CredentialCleanupDays"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "CredentialCleanupDays", Value = "90" });
                logger.LogInformation("Default CredentialCleanupDays setting seeded successfully.");
            }

            if (!await context.SiteSettings.AnyAsync(s => s.Key == "EnableCredentialAutoCleanup"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "EnableCredentialAutoCleanup", Value = "true" });
                logger.LogInformation("Default EnableCredentialAutoCleanup setting seeded successfully.");
            }

            // Content Moderation Settings
            if (!await context.SiteSettings.AnyAsync(s => s.Key == "ContentEditWindowMinutes"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "ContentEditWindowMinutes", Value = "15" });
                logger.LogInformation("Default ContentEditWindowMinutes setting seeded successfully.");
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
            List<Torrent> torrents = await SeedTorrentsAsync(context, logger, users, tmdbService, env);

            // Seed Comments (using unified Comment system)
            await SeedCommentsAsync(context, logger, users, torrents, 100); // Seed 100 comments

            // Seed Messages
            await SeedMessagesAsync(context, logger, users, 50); // Seed 50 messages

            // Seed Announcements
            await SeedAnnouncementsAsync(context, logger,
                users.Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Moderator).ToList(),
                5); // Seed 5 announcements

            var badges = await context.Badges.ToListAsync();

            // Seed Requests
            var requests = await SeedRequestsAsync(context, logger, users, torrents, 20); // Seed 20 requests
            
            // Seed Request Comments (using unified Comment system)
            await SeedRequestCommentsAsync(context, logger, users, requests, 80); // Seed 80 request comments

            // Seed Reports
            await SeedReportsAsync(context, logger, users, torrents, 30); // Seed 30 reports

            // Seed Peers
            await SeedPeersAsync(context, logger, users, torrents, 100); // Seed 100 peers

            // Seed TorrentCredentials
            await SeedTorrentCredentialsAsync(context, logger, users, torrents, 50); // Seed 50 credentials

            // Seed UserBadges
            await SeedUserBadgesAsync(context, logger, users, badges, 30); // Assign 30 user badges

            // Seed UserDailyStats
            await SeedUserDailyStatsAsync(context, logger, users, 50); // Seed 50 daily stats entries

            // Seed Forum Data (using unified Comment system)
            await SeedForumDataAsync(context, logger, users);

            // Seed Polls
            await SeedPollsAsync(context, logger, users);

            // Seed Comment Reactions (using unified Comment system)
            await SeedCommentReactionsAsync(context, logger, users);

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
                    IsEmailVerified = true,
                    TwoFactorType = TwoFactorType.Email
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

                    // Use relative path to avoid WebRootPath null issue
                    var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var avatarDirectory = Path.Combine(webRootPath, "avatars");
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
                .RuleFor(u => u.Language, f => f.PickRandom("en", "zh-CN", "ja", "fr"))
                .RuleFor(u => u.UploadedBytes, f => (ulong)f.Random.Long(0, 100_000_000_000)) // Up to 100 GB
                .RuleFor(u => u.DownloadedBytes,
                    (f, u) => (ulong)f.Random.Long(0, (long)u.UploadedBytes)) // Downloaded less than uploaded
                .RuleFor(u => u.NominalUploadedBytes, (f, u) => u.UploadedBytes)
                .RuleFor(u => u.NominalDownloadedBytes, (f, u) => u.DownloadedBytes)
                .RuleFor(u => u.Role, f => f.PickRandom<UserRole>(UserRole.User, UserRole.Moderator))
                .RuleFor(u => u.IsEmailVerified, f => true)
                .RuleFor(u => u.TwoFactorType, f => TwoFactorType.Email)
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(5).ToUniversalTime())
                .RuleFor(u => u.BanStatus, f => f.Random.Bool(0.1f) ? f.PickRandom<BanStatus>() : BanStatus.None)
                .RuleFor(u => u.CheatWarningCount, f => 0) // Default to 0 for seed data
                .RuleFor(u => u.BanReason, (f, u) => u.BanStatus != BanStatus.None ? f.Lorem.Sentence(50).Substring(0, 50) : null)
                .RuleFor(u => u.BanUntil, (f, u) => u.BanStatus != BanStatus.None ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.InviteNum, f => f.Random.UInt(0, 5))
                .RuleFor(u => u.Coins, f => (ulong)f.Random.Long(0, 1000))
                .RuleFor(u => u.TotalSeedingTimeMinutes, f => (ulong)f.Random.Long(0, 10000))
                .RuleFor(u => u.TotalLeechingTimeMinutes, f => (ulong)f.Random.Long(0, 5000))
                .RuleFor(u => u.IsDoubleUploadActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.DoubleUploadExpiresAt,
                    (f, u) => u.IsDoubleUploadActive ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.IsNoHRActive, f => f.Random.Bool(0.05f))
                .RuleFor(u => u.NoHRExpiresAt,
                    (f, u) => u.IsNoHRActive ? f.Date.Future(1).ToUniversalTime() : (DateTime?)null)
                .RuleFor(u => u.UserTitle, f => f.Random.Bool(0.2f) ? f.Lorem.Word() : null)
                .RuleFor(u => u.EquippedBadgeId, f => null) // Will be set randomly in SeedUserBadgesAsync
                .RuleFor(u => u.ColorfulUsernameExpiresAt, f =>
                    f.Random.Bool(0.1f) ? f.Date.Future(1).ToUniversalTime() : (DateTimeOffset?)null)
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
            List<User> users, ITMDbService tmdbService, IWebHostEnvironment env)
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
            var mediaInputParser = new MediaInputParser();

            foreach (var movieId in movieIds)
            {
                try
                {
                    var movie = await tmdbService.GetMovieByTmdbIdAsync(movieId, "zh-CN");
                    if (movie != null)
                    {
                        var year = !string.IsNullOrEmpty(movie.ReleaseDate) && DateTime.TryParse(movie.ReleaseDate, out var releaseDate) ? releaseDate.Year.ToString() : "2022";
                        var resolution = faker.PickRandom("1080p", "2160p", "720p");
                        var source = faker.PickRandom("BluRay", "WEB-DL", "HDTV");
                        var videoCodec = faker.PickRandom("x264", "x265");
                        var audioCodec = faker.PickRandom("DTS-HD.MA.5.1", "TrueHD.7.1.Atmos", "AAC");

                        var realisticName = $"{movie.Title?.Replace(":", "")} {year} {resolution} {source} {videoCodec} {audioCodec}-PTer";
                        var techSpecs = mediaInputParser.ParseTechnicalSpecs(realisticName);

                        var infoHashBytes = faker.Random.Bytes(20);
                        var torrent = new Torrent
                        {
                            Name = realisticName,
                            InfoHash = infoHashBytes,
                            FilePath = Path.Combine(env.WebRootPath ?? "wwwroot", "torrents", $"{BitConverter.ToString(infoHashBytes).Replace("-", "").ToLowerInvariant()}.torrent"),
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
                                   DateTime.TryParse(movie.ReleaseDate, out var parsedReleaseDate)
                                ? parsedReleaseDate.Year
                                : null,
                            PosterPath = movie.PosterPath,
                            BackdropPath = movie.BackdropPath,
                            Runtime = movie.Runtime,
                            Genres = movie.Genres.Select(g => g.Name).ToList(),
                            Rating = movie.VoteAverage,
                            Country = movie.ProductionCountries?.FirstOrDefault()?.Name,
                            Cast = movie.Credits.Cast.Select(c => new CastMemberDto
                            {
                                Name = c.Name,
                                Character = c.Character,
                                ProfilePath = c.ProfilePath
                            }).ToList(),
                            Resolution = techSpecs?.Resolution,
                            VideoCodec = techSpecs?.VideoCodec,
                            AudioCodec = techSpecs?.AudioCodec,
                            Source = techSpecs?.Source
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

                // Create dummy .torrent files
                foreach (var torrent in torrents)
                {
                    if (!File.Exists(torrent.FilePath))
                    {
                        var directory = Path.GetDirectoryName(torrent.FilePath);
                        if (directory != null && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        File.WriteAllText(torrent.FilePath, "d8:announce0:e");
                    }
                }
            }

            return torrents;
        }


        public static async Task SeedCommentsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Torrent> torrents, int count)
        {
            if (!await context.Comments.AnyAsync(c => c.CommentableType == CommentableType.Torrent) && torrents.Any() && users.Any())
            {
                var faker = new Faker();
                var totalComments = 0;

                foreach (var torrent in torrents.Take(5)) // 为前5个种子生成评论
                {
                    var commentsForTorrent = new List<Comment>();
                    var topLevelCount = faker.Random.Int(5, 15); // 每个种子5-15条顶级评论
                    int currentFloor = 1;

                    // 生成顶级评论
                    for (int i = 0; i < topLevelCount; i++)
                    {
                        var topLevelComment = new Comment
                        {
                            Content = faker.Lorem.Sentence(faker.Random.Int(5, 15)),
                            CommentableType = CommentableType.Torrent,
                            CommentableId = torrent.Id,
                            UserId = faker.PickRandom(users).Id,
                            CreatedAt = faker.Date.Past(1).ToUniversalTime(),
                            Floor = currentFloor++,
                            ParentCommentId = null,
                            ReplyToUserId = null,
                            Depth = 0,
                            ReplyCount = 0
                        };
                        commentsForTorrent.Add(topLevelComment);
                    }

                    // 保存顶级评论以获取ID
                    context.Comments.AddRange(commentsForTorrent);
                    await context.SaveChangesAsync();

                    // 为部分顶级评论生成回复(30%概率)
                    foreach (var parentComment in commentsForTorrent.Where(c => c.Depth == 0).OrderBy(c => c.Floor))
                    {
                        if (faker.Random.Bool(0.3f)) // 30%的顶级评论有回复
                        {
                            var replyCount = faker.Random.Int(1, 3); // 1-3条回复
                            var replies = new List<Comment>();

                            for (int i = 0; i < replyCount; i++)
                            {
                                var replyUserId = faker.PickRandom(users.Where(u => u.Id != parentComment.UserId).ToList()).Id;
                                var reply = new Comment
                                {
                                    Content = $"@{parentComment.User?.UserName ?? "User"} {faker.Lorem.Sentence(faker.Random.Int(3, 10))}",
                                    CommentableType = CommentableType.Torrent,
                                    CommentableId = torrent.Id,
                                    UserId = replyUserId,
                                    CreatedAt = faker.Date.Between(parentComment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                    Floor = currentFloor++,
                                    ParentCommentId = parentComment.Id,
                                    ReplyToUserId = parentComment.UserId,
                                    Depth = 1,
                                    ReplyCount = 0
                                };
                                replies.Add(reply);
                            }

                            context.Comments.AddRange(replies);
                            parentComment.ReplyCount = replyCount;
                            
                            // 先保存二级回复以获取ID
                            await context.SaveChangesAsync();
                            
                            // 为二级回复生成三级回复(10%概率)
                            foreach (var secondLevelReply in replies)
                            {
                                if (faker.Random.Bool(0.1f) && secondLevelReply.Depth < 2)
                                {
                                    var thirdLevelUserId = faker.PickRandom(users.Where(u => u.Id != secondLevelReply.UserId).ToList()).Id;
                                    var thirdLevelReply = new Comment
                                    {
                                        Content = $"@{secondLevelReply.User?.UserName ?? "User"} {faker.Lorem.Sentence(faker.Random.Int(3, 8))}",
                                        CommentableType = CommentableType.Torrent,
                                        CommentableId = torrent.Id,
                                        UserId = thirdLevelUserId,
                                        CreatedAt = faker.Date.Between(secondLevelReply.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                        Floor = currentFloor++,
                                        ParentCommentId = secondLevelReply.Id,
                                        ReplyToUserId = secondLevelReply.UserId,
                                        Depth = 2,
                                        ReplyCount = 0
                                    };
                                    context.Comments.Add(thirdLevelReply);
                                    secondLevelReply.ReplyCount++;
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                    totalComments += commentsForTorrent.Count;
                }

                logger.LogInformation("{Count} torrent comments with replies seeded successfully.", totalComments);
            }
            else
            {
                logger.LogInformation("Torrent comments already exist or no torrents/users to comment on, skipping seeding.");
            }
        }

        public static async Task SeedMessagesAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            int count)
        {
            if (!await context.Messages.AnyAsync() && users.Count >= 2)
            {
                var messageFaker = new Faker<Message>()
                    .RuleFor(m => m.SenderId, f =>
                    {
                        var sender = f.PickRandom(users);
                        return sender.Id;
                    })
                    .RuleFor(m => m.Sender, (f, m) => users.First(u => u.Id == m.SenderId))
                    .RuleFor(m => m.ReceiverId, (f, m) =>
                    {
                        var receiver = f.PickRandom(users.Where(u => u.Id != m.SenderId).ToList());
                        return receiver.Id;
                    })
                    .RuleFor(m => m.Receiver, (f, m) => users.First(u => u.Id == m.ReceiverId))
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
                    .RuleFor(a => a.CreatedByUserId, f =>
                    {
                        var user = f.PickRandom(adminModerators);
                        return user.Id;
                    })
                    .RuleFor(a => a.CreatedByUser, (f, a) => adminModerators.First(u => u.Id == a.CreatedByUserId));

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

        public static async Task<List<Request>> SeedRequestsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
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
                return requests;
            }
            else
            {
                logger.LogInformation("Requests already exist or no users, skipping seeding.");
                return await context.Requests.ToListAsync();
            }
        }

        public static async Task SeedRequestCommentsAsync(ApplicationDbContext context, ILogger logger, List<User> users,
            List<Request> requests, int count)
        {
            if (!await context.Comments.AnyAsync(c => c.CommentableType == CommentableType.Request) && requests.Any() && users.Any())
            {
                var faker = new Faker();
                var totalComments = 0;

                foreach (var request in requests.Take(8)) // 为前8个求种生成评论
                {
                    var commentsForRequest = new List<Comment>();
                    var topLevelCount = faker.Random.Int(3, 12); // 每个求种3-12条顶级评论
                    int currentFloor = 1;

                    // 生成顶级评论
                    for (int i = 0; i < topLevelCount; i++)
                    {
                        var topLevelComment = new Comment
                        {
                            Content = faker.Lorem.Sentence(faker.Random.Int(5, 15)),
                            CommentableType = CommentableType.Request,
                            CommentableId = request.Id,
                            UserId = faker.PickRandom(users).Id,
                            CreatedAt = faker.Date.Between(request.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                            Floor = currentFloor++,
                            ParentCommentId = null,
                            ReplyToUserId = null,
                            Depth = 0,
                            ReplyCount = 0
                        };
                        commentsForRequest.Add(topLevelComment);
                    }

                    // 保存顶级评论以获取ID
                    context.Comments.AddRange(commentsForRequest);
                    await context.SaveChangesAsync();

                    // 为部分顶级评论生成回复(40%概率)
                    foreach (var parentComment in commentsForRequest.Where(c => c.Depth == 0).OrderBy(c => c.Floor))
                    {
                        if (faker.Random.Bool(0.4f)) // 40%的顶级评论有回复
                        {
                            var replyCount = faker.Random.Int(1, 4); // 1-4条回复
                            var replies = new List<Comment>();

                            for (int i = 0; i < replyCount; i++)
                            {
                                var replyUserId = faker.PickRandom(users.Where(u => u.Id != parentComment.UserId).ToList()).Id;
                                var reply = new Comment
                                {
                                    Content = $"@{parentComment.User?.UserName ?? "User"} {faker.Lorem.Sentence(faker.Random.Int(3, 10))}",
                                    CommentableType = CommentableType.Request,
                                    CommentableId = request.Id,
                                    UserId = replyUserId,
                                    CreatedAt = faker.Date.Between(parentComment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                    Floor = currentFloor++,
                                    ParentCommentId = parentComment.Id,
                                    ReplyToUserId = parentComment.UserId,
                                    Depth = 1,
                                    ReplyCount = 0
                                };
                                replies.Add(reply);
                            }

                            context.Comments.AddRange(replies);
                            parentComment.ReplyCount = replyCount;
                            
                            // 先保存二级回复以获取ID
                            await context.SaveChangesAsync();
                            
                            // 为二级回复生成三级回复(15%概率)
                            foreach (var secondLevelReply in replies)
                            {
                                if (faker.Random.Bool(0.15f) && secondLevelReply.Depth < 2)
                                {
                                    var thirdLevelUserId = faker.PickRandom(users.Where(u => u.Id != secondLevelReply.UserId).ToList()).Id;
                                    var thirdLevelReply = new Comment
                                    {
                                        Content = $"@{secondLevelReply.User?.UserName ?? "User"} {faker.Lorem.Sentence(faker.Random.Int(3, 8))}",
                                        CommentableType = CommentableType.Request,
                                        CommentableId = request.Id,
                                        UserId = thirdLevelUserId,
                                        CreatedAt = faker.Date.Between(secondLevelReply.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                        Floor = currentFloor++,
                                        ParentCommentId = secondLevelReply.Id,
                                        ReplyToUserId = secondLevelReply.UserId,
                                        Depth = 2,
                                        ReplyCount = 0
                                    };
                                    context.Comments.Add(thirdLevelReply);
                                    secondLevelReply.ReplyCount++;
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                    totalComments += commentsForRequest.Count;
                }

                logger.LogInformation("{Count} request comments with replies seeded successfully.", totalComments);
            }
            else
            {
                logger.LogInformation("Request comments already exist or no requests/users to comment on, skipping seeding.");
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
                    UserAgent = (new Func<string>(() => { var ua = faker.Internet.UserAgent(); return ua.Substring(0, Math.Min(ua.Length, 100)); }))(), // Updated to 100 chars to match Entity definition
                    Uploaded = (ulong)faker.Random.Long(0, 100_000_000_000), // 0-100GB
                    Downloaded = (ulong)faker.Random.Long(0, 100_000_000_000) // 0-100GB
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

        public static async Task SeedTorrentCredentialsAsync(ApplicationDbContext context, ILogger logger,
            List<User> users, List<Torrent> torrents, int count)
        {
            if (!await context.TorrentCredentials.AnyAsync() && users.Any() && torrents.Any())
            {
                var faker = new Faker();
                var credentials = new List<TorrentCredential>();

                // 为每个用户随机生成几个种子的credentials
                var allPossiblePairs = users.SelectMany(user => torrents.Select(torrent => new { user, torrent }))
                    .ToList();

                var uniquePairs = faker.PickRandom(allPossiblePairs, Math.Min(count, allPossiblePairs.Count)).ToList();

                foreach (var pair in uniquePairs)
                {
                    var credential = new TorrentCredential
                    {
                        Credential = Guid.NewGuid(),
                        UserId = pair.user.Id,
                        User = pair.user,
                        TorrentId = pair.torrent.Id,
                        Torrent = pair.torrent,
                        CreatedAt = faker.Date.Past(1).ToUniversalTime(),
                        LastUsedAt = faker.Random.Bool(0.7f)
                            ? faker.Date.Recent(7).ToUniversalTime()
                            : (DateTimeOffset?)null, // 70%有使用记录
                        IsRevoked = faker.Random.Bool(0.1f), // 10%被撤销
                        RevokedAt = null,
                        RevokeReason = null,
                        UsageCount = faker.Random.Int(0, 100)
                    };

                    // 如果已撤销,设置撤销信息
                    if (credential.IsRevoked)
                    {
                        credential.RevokedAt = faker.Date.Between(
                            credential.CreatedAt.UtcDateTime,
                            DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime();
                        credential.RevokeReason = faker.PickRandom(
                            "User requested",
                            "Abuse detected",
                            "Torrent removed",
                            "Account suspended");
                    }

                    credentials.Add(credential);
                }

                context.TorrentCredentials.AddRange(credentials);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} torrent credentials seeded successfully.", credentials.Count);

                // 为部分Peers添加credential引用
                var peers = await context.Peers
                    .Include(p => p.User)
                    .Include(p => p.Torrent)
                    .ToListAsync();

                foreach (var peer in peers.Take(Math.Min(30, peers.Count)))
                {
                    // 查找对应的credential
                    var matchingCredential = credentials.FirstOrDefault(c =>
                        c.UserId == peer.UserId &&
                        c.TorrentId == peer.TorrentId &&
                        !c.IsRevoked);

                    if (matchingCredential != null)
                    {
                        peer.Credential = matchingCredential.Credential;
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Updated {Count} peers with credential references.",
                    peers.Count(p => p.Credential != null));
            }
            else
            {
                logger.LogInformation("Torrent credentials already exist or no users/torrents, skipping seeding.");
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

            // 2. Seed Forum Topics and Posts (using unified Comment system)
            if (!await context.ForumTopics.AnyAsync() && users.Any() && forumCategories.Any())
            {
                var faker = new Faker();
                var topicFaker = new Faker<ForumTopic>()
                    .RuleFor(t => t.Title, f => f.Lorem.Sentence(5))
                    .RuleFor(t => t.Author, f => f.PickRandom(users))
                    .RuleFor(t => t.AuthorId, (f, t) => t.Author!.Id) // 确保AuthorId被正确设置
                    .RuleFor(t => t.Category, f => f.PickRandom(forumCategories))
                    .RuleFor(t => t.CategoryId, (f, t) => t.Category!.Id) // 确保CategoryId被正确设置
                    .RuleFor(t => t.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
                    .RuleFor(t => t.IsSticky, f => f.Random.Bool(0.1f))
                    .RuleFor(t => t.IsLocked, f => f.Random.Bool(0.05f));

                var topics = topicFaker.Generate(25); // Create 25 topics
                
                // 先保存所有主题以获取ID
                context.ForumTopics.AddRange(topics);
                await context.SaveChangesAsync();
                
                // 全局楼层计数器，确保所有评论的楼层号都是唯一的
                int globalFloorCounter = 1;

                foreach (var topic in topics)
                {
                    var commentsForTopic = new List<Comment>();
                    var topLevelCount = faker.Random.Int(3, 12);

                    // 生成顶级帖子
                    for (int i = 0; i < topLevelCount; i++)
                    {
                        var content = string.Join("\n", faker.Lorem.Paragraphs(faker.Random.Int(1, 4)));
                        var topLevelComment = new Comment
                        {
                            Content = content.Substring(0, Math.Min(content.Length, 1000)),
                            CommentableType = CommentableType.ForumTopic,
                            CommentableId = topic.Id,
                            UserId = i == 0 ? topic.AuthorId : faker.PickRandom(users).Id,
                            CreatedAt = i == 0 ? topic.CreatedAt : faker.Date.Between(topic.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                            Floor = globalFloorCounter++,
                            ParentCommentId = null,
                            ReplyToUserId = null,
                            Depth = 0,
                            ReplyCount = 0
                        };
                        commentsForTopic.Add(topLevelComment);
                    }

                    context.Comments.AddRange(commentsForTopic);
                    await context.SaveChangesAsync();

                    // 为部分顶级帖子生成回复
                    foreach (var parentComment in commentsForTopic.Where(c => c.Depth == 0).OrderBy(c => c.Floor))
                    {
                        if (faker.Random.Bool(0.4f))
                        {
                            var replyCount = faker.Random.Int(1, 4);
                            var replies = new List<Comment>();

                            for (int i = 0; i < replyCount; i++)
                            {
                                var replyUserId = faker.PickRandom(users.Where(u => u.Id != parentComment.UserId).ToList()).Id;
                                var replyContent = string.Join("\n", faker.Lorem.Paragraphs(faker.Random.Int(1, 2)));
                                var reply = new Comment
                                {
                                    Content = $"@{parentComment.User?.UserName ?? "User"}\n{replyContent.Substring(0, Math.Min(replyContent.Length, 800))}",
                                    CommentableType = CommentableType.ForumTopic,
                                    CommentableId = topic.Id,
                                    UserId = replyUserId,
                                    CreatedAt = faker.Date.Between(parentComment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                    Floor = globalFloorCounter++,
                                    ParentCommentId = parentComment.Id,
                                    ReplyToUserId = parentComment.UserId,
                                    Depth = 1,
                                    ReplyCount = 0
                                };
                                replies.Add(reply);
                            }

                            context.Comments.AddRange(replies);
                            parentComment.ReplyCount = replyCount;

                            // 先保存二级回复以获取ID
                            await context.SaveChangesAsync();

                            foreach (var secondLevelReply in replies)
                            {
                                if (faker.Random.Bool(0.15f) && secondLevelReply.Depth < 2)
                                {
                                    var thirdLevelUserId = faker.PickRandom(users.Where(u => u.Id != secondLevelReply.UserId).ToList()).Id;
                                    var thirdContent = faker.Lorem.Sentence(faker.Random.Int(3, 8));
                                    var thirdLevelReply = new Comment
                                    {
                                        Content = $"@{secondLevelReply.User?.UserName ?? "User"} {thirdContent}",
                                        CommentableType = CommentableType.ForumTopic,
                                        CommentableId = topic.Id,
                                        UserId = thirdLevelUserId,
                                        CreatedAt = faker.Date.Between(secondLevelReply.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime(),
                                        Floor = globalFloorCounter++,
                                        ParentCommentId = secondLevelReply.Id,
                                        ReplyToUserId = secondLevelReply.UserId,
                                        Depth = 2,
                                        ReplyCount = 0
                                    };
                                    context.Comments.Add(thirdLevelReply);
                                    secondLevelReply.ReplyCount++;
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                    var maxCreatedAt = await context.Comments
                        .Where(c => c.CommentableType == CommentableType.ForumTopic && c.CommentableId == topic.Id)
                        .MaxAsync(c => (DateTimeOffset?)c.CreatedAt);
                    topic.LastPostTime = (maxCreatedAt ?? topic.CreatedAt).UtcDateTime;
                }

                await context.SaveChangesAsync();
                logger.LogInformation("{TopicCount} forum topics with posts and replies seeded successfully.", topics.Count);
            }
            else
            {
                logger.LogInformation("Forum topics already exist or no users/categories, skipping seeding.");
            }

            logger.LogInformation("All mock data seeding completed.");
        }

        public static async Task SeedPollsAsync(ApplicationDbContext context, ILogger logger, List<User> users)
        {
            if (!await context.Polls.AnyAsync() && users.Any())
            {
                var pollFaker = new Faker<Poll>()
                    .RuleFor(p => p.Question, f => "What's your favorite torrent category?")
                    .RuleFor(p => p.Options, f => new List<string> { "Movies", "TV Shows", "Music", "Games", "Software" })
                    .RuleFor(p => p.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
                    .RuleFor(p => p.ExpiresAt, f => DateTimeOffset.UtcNow.AddDays(7)); // Make sure it's active

                var polls = pollFaker.Generate(1); // Create 1 poll
                context.Polls.AddRange(polls);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} polls seeded successfully.", polls.Count);
            }
            else
            {
                logger.LogInformation("Polls already exist or no users, skipping seeding.");
            }
        }

        public static async Task SeedCommentReactionsAsync(ApplicationDbContext context, ILogger logger, List<User> users)
        {
            var existingReactionsCount = await context.CommentReactions.CountAsync();
            logger.LogInformation("Existing comment reactions count: {Count}", existingReactionsCount);
            logger.LogInformation("Users count: {Count}", users.Count);
            
            if (existingReactionsCount == 0 && users.Any())
            {
                var faker = new Faker();
                var reactions = new List<CommentReaction>();

                // 为种子评论添加反应
                var torrentComments = await context.Comments
                    .Where(c => c.CommentableType == CommentableType.Torrent && c.Depth == 0) // 只为顶级评论添加反应
                    .Take(20) // 取前20条评论
                    .ToListAsync();

                logger.LogInformation("Found {Count} torrent comments for reactions", torrentComments.Count);

                foreach (var comment in torrentComments)
                {
                    // 每条评论随机添加3-8个反应（但不超过用户数量）
                    var reactionCount = faker.Random.Int(3, Math.Min(8, users.Count));
                    var commentUsers = faker.PickRandom(users, reactionCount).ToList();

                    foreach (var user in commentUsers)
                    {
                        var reactionType = faker.PickRandom<ReactionType>();
                        
                        // 确保唯一性：同一用户对同一评论只能有一种反应类型
                        if (!reactions.Any(r => r.CommentId == comment.Id &&
                                               r.UserId == user.Id &&
                                               r.Type == reactionType))
                        {
                            reactions.Add(new CommentReaction
                            {
                                CommentType = "TorrentComment",
                                CommentId = comment.Id,
                                UserId = user.Id,
                                Type = reactionType,
                                CreatedAt = faker.Date.Between(comment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime()
                            });
                        }
                    }
                }

                // 为论坛帖子添加反应
                var forumComments = await context.Comments
                    .Where(c => c.CommentableType == CommentableType.ForumTopic && c.Depth == 0) // 只为顶级帖子添加反应
                    .Take(30) // 取前30条帖子
                    .ToListAsync();

                foreach (var comment in forumComments)
                {
                    // 每条帖子随机添加5-12个反应（但不超过用户数量）
                    var reactionCount = faker.Random.Int(5, Math.Min(12, users.Count));
                    var commentUsers = faker.PickRandom(users, reactionCount).ToList();

                    foreach (var user in commentUsers)
                    {
                        var reactionType = faker.PickRandom<ReactionType>();
                        
                        // 确保唯一性
                        if (!reactions.Any(r => r.CommentId == comment.Id &&
                                               r.UserId == user.Id &&
                                               r.Type == reactionType))
                        {
                            reactions.Add(new CommentReaction
                            {
                                CommentType = "ForumPost",
                                CommentId = comment.Id,
                                UserId = user.Id,
                                Type = reactionType,
                                CreatedAt = faker.Date.Between(comment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime()
                            });
                        }
                    }
                }

                // 为求种评论添加反应
                var requestComments = await context.Comments
                    .Where(c => c.CommentableType == CommentableType.Request && c.Depth == 0) // 只为顶级评论添加反应
                    .Take(25) // 取前25条评论
                    .ToListAsync();

                foreach (var comment in requestComments)
                {
                    // 每条评论随机添加4-10个反应（但不超过用户数量）
                    var reactionCount = faker.Random.Int(4, Math.Min(10, users.Count));
                    var commentUsers = faker.PickRandom(users, reactionCount).ToList();

                    foreach (var user in commentUsers)
                    {
                        var reactionType = faker.PickRandom<ReactionType>();
                        
                        // 确保唯一性
                        if (!reactions.Any(r => r.CommentId == comment.Id &&
                                               r.UserId == user.Id &&
                                               r.Type == reactionType))
                        {
                            reactions.Add(new CommentReaction
                            {
                                CommentType = "RequestComment",
                                CommentId = comment.Id,
                                UserId = user.Id,
                                Type = reactionType,
                                CreatedAt = faker.Date.Between(comment.CreatedAt.UtcDateTime, DateTimeOffset.UtcNow.UtcDateTime).ToUniversalTime()
                            });
                        }
                    }
                }

                if (reactions.Any())
                {
                    context.CommentReactions.AddRange(reactions);
                    await context.SaveChangesAsync();
                    logger.LogInformation("{Count} comment reactions seeded successfully (TorrentComments: {TorrentCount}, ForumComments: {ForumCount}, RequestComments: {RequestCount}).",
                        reactions.Count,
                        reactions.Count(r => r.CommentType == "TorrentComment"),
                        reactions.Count(r => r.CommentType == "ForumPost"),
                        reactions.Count(r => r.CommentType == "RequestComment"));
                }
            }
            else
            {
                logger.LogInformation("Comment reactions already exist or no users, skipping seeding.");
            }
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