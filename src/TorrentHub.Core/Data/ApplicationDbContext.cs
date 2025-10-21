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
        public DbSet<TorrentComment> TorrentComments { get; set; }
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
        public DbSet<CommentReaction> CommentReactions { get; set; }
 
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
            modelBuilder.HasPostgresEnum<ReactionType>();

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

            // ForumPost reply relationships
            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.ReplyToUser)
                .WithMany()
                .HasForeignKey(p => p.ReplyToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ForumPost>()
                .HasIndex(p => new { p.TopicId, p.Floor })
                .IsUnique();

            modelBuilder.Entity<ForumPost>()
                .HasIndex(p => p.ParentPostId);

            // TorrentComment reply relationships
            modelBuilder.Entity<TorrentComment>()
                .HasOne(c => c.ParentTorrentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TorrentComment>()
                .HasOne(c => c.ReplyToUser)
                .WithMany()
                .HasForeignKey(c => c.ReplyToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TorrentComment>()
                .HasIndex(c => new { c.TorrentId, c.Floor })
                .IsUnique();

            modelBuilder.Entity<TorrentComment>()
                .HasIndex(c => c.ParentCommentId);

            modelBuilder.Entity<SiteSetting>()
                .Property(s => s.Value)
                .HasColumnType("jsonb");

            // CommentReaction configuration
            modelBuilder.Entity<CommentReaction>()
                .HasIndex(r => new { r.CommentType, r.CommentId })
                .HasDatabaseName("IX_CommentReactions_Comment");

            modelBuilder.Entity<CommentReaction>()
                .HasIndex(r => new { r.CommentType, r.CommentId, r.UserId, r.Type })
                .IsUnique()
                .HasDatabaseName("IX_CommentReactions_Unique");

            modelBuilder.Entity<CommentReaction>()
                .HasIndex(r => r.UserId)
                .HasDatabaseName("IX_CommentReactions_UserId");

            modelBuilder.Entity<CommentReaction>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

