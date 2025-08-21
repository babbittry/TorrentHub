using Microsoft.EntityFrameworkCore;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Torrent> Torrents { get; set; }
        public DbSet<Invite> Invites { get; set; }
        public DbSet<Peers> Peers { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserDailyStats> UserDailyStats { get; set; }
        public DbSet<StoreItem> StoreItems { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<ForumCategory> ForumCategories { get; set; }
        public DbSet<ForumTopic> ForumTopics { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Torrent>()
                .HasIndex(t => t.InfoHash)
                .IsUnique();

            modelBuilder.Entity<Invite>()
                .HasIndex(i => i.Id)
                .IsUnique();

            modelBuilder.Entity<Invite>()
                .HasOne(i => i.GeneratorUser)
                .WithMany(u => u.GeneratedInvites)
                .HasForeignKey(i => i.GeneratorUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent circular cascade delete

            modelBuilder.Entity<Invite>()
                .HasOne(i => i.UsedByUser)
                .WithOne(u => u.Invite)
                .HasForeignKey<User>(u => u.InviteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserDailyStats>()
                .HasIndex(s => new { s.UserId, s.Date })
                .IsUnique();

            // Seed the store with default items
            modelBuilder.Entity<StoreItem>().HasData(
                new StoreItem { Id = 1, ItemCode = Enums.StoreItemCode.UploadCredit10GB, Price = 1000, IsAvailable = true },
                new StoreItem { Id = 2, ItemCode = Enums.StoreItemCode.UploadCredit50GB, Price = 4500, IsAvailable = true },
                new StoreItem { Id = 3, ItemCode = Enums.StoreItemCode.InviteOne, Price = 5000, IsAvailable = true },
                new StoreItem { Id = 4, ItemCode = Enums.StoreItemCode.InviteFive, Price = 20000, IsAvailable = true },
                new StoreItem { Id = 5, ItemCode = Enums.StoreItemCode.DoubleUpload, Price = 10000, IsAvailable = true },
                new StoreItem { Id = 6, ItemCode = Enums.StoreItemCode.NoHitAndRun, Price = 15000, IsAvailable = true },
                new StoreItem { Id = 7, ItemCode = Enums.StoreItemCode.Badge, Price = 25000, IsAvailable = true, BadgeId = 4 }
            );

            // Seed some badges
            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = 1, Code = BadgeCode.EarlySupporter },
                new Badge { Id = 2, Code = BadgeCode.TorrentMaster },
                new Badge { Id = 3, Code = BadgeCode.CommunityContributor },
                new Badge { Id = 4, Code = BadgeCode.CoinCollector }
            );

            modelBuilder.Entity<UserBadge>()
                .HasIndex(ub => new { ub.UserId, ub.BadgeId })
                .IsUnique();

            modelBuilder.Entity<ForumCategory>().HasData(
                new ForumCategory { Id = 1, Code = ForumCategoryCode.Announcement, DisplayOrder = 1 },
                new ForumCategory { Id = 2, Code = ForumCategoryCode.General, DisplayOrder = 2 },
                new ForumCategory { Id = 3, Code = ForumCategoryCode.Feedback, DisplayOrder = 3 },
                new ForumCategory { Id = 4, Code = ForumCategoryCode.Invite, DisplayOrder = 4 },
                new ForumCategory { Id = 5, Code = ForumCategoryCode.Watering, DisplayOrder = 5 }
            );

            modelBuilder.Entity<ForumTopic>()
                .HasOne(t => t.Author)
                .WithMany()
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ForumTopic>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.Topic)
                .WithMany(t => t.Posts)
                .HasForeignKey(p => p.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
