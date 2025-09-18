using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Data
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

        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<BannedClient> BannedClients { get; set; }
        public DbSet<CheatLog> CheatLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CoinTransaction> CoinTransactions { get; set; }
 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<TransactionType>();
            modelBuilder.HasPostgresEnum<UserRole>();
            modelBuilder.HasPostgresEnum<BadgeCode>();
            modelBuilder.HasPostgresEnum<ForumCategoryCode>();
            modelBuilder.HasPostgresEnum<ReportReason>();
            modelBuilder.HasPostgresEnum<RequestStatus>();
            modelBuilder.HasPostgresEnum<StoreItemCode>();
            modelBuilder.HasPostgresEnum<TorrentCategory>();
            modelBuilder.HasPostgresEnum<TorrentStickyStatus>();
            modelBuilder.HasPostgresEnum<BanStatus>();

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });
 
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(rt => rt.TokenHash).IsUnique();
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

            modelBuilder.Entity<UserBadge>()
                .HasIndex(ub => new { ub.UserId, ub.BadgeId })
                .IsUnique();
 
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

            modelBuilder.Entity<SiteSetting>()
                .Property(s => s.Value)
                .HasColumnType("jsonb");
        }
    }
}

