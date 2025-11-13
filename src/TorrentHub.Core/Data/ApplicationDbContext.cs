using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.DTOs;
using System.Text.Json;

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
        public DbSet<RequestComment> RequestComments { get; set; }
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
        public DbSet<TorrentCredential> TorrentCredentials { get; set; }
        public DbSet<RssFeedToken> RssFeedTokens { get; set; }
 
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

            // RequestComment reply relationships
            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.ParentRequestComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.ReplyToUser)
                .WithMany()
                .HasForeignKey(c => c.ReplyToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RequestComment>()
                .HasIndex(c => new { c.RequestId, c.Floor })
                .IsUnique();

            modelBuilder.Entity<RequestComment>()
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
                
            modelBuilder.Entity<TorrentCredential>(entity =>
            {
                entity.HasIndex(e => e.Credential).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.TorrentId, e.IsRevoked }).IsUnique().HasFilter("\"IsRevoked\" = false");
            });
            
            // CheatLog configuration
            modelBuilder.Entity<CheatLog>(entity =>
            {
                // 单列索引 - 用于单一条件查询
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_CheatLogs_UserId");
                      
                entity.HasIndex(e => e.DetectionType)
                      .HasDatabaseName("IX_CheatLogs_DetectionType");
                      
                entity.HasIndex(e => e.TorrentId)
                      .HasDatabaseName("IX_CheatLogs_TorrentId")
                      .HasFilter("\"TorrentId\" IS NOT NULL");
                      
                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_CheatLogs_Timestamp")
                      .IsDescending();
                
                entity.HasIndex(e => e.Severity)
                      .HasDatabaseName("IX_CheatLogs_Severity");
                
                // 组合索引 - 覆盖最常见查询模式
                entity.HasIndex(e => new { e.UserId, e.DetectionType, e.Timestamp })
                      .HasDatabaseName("IX_CheatLogs_UserId_DetectionType_Timestamp")
                      .IsDescending(false, false, true);
            });
            
            // RssFeedToken configuration
            modelBuilder.Entity<RssFeedToken>(entity =>
            {
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsActive).HasFilter("\"IsActive\" = true");
                entity.HasIndex(e => e.ExpiresAt).HasFilter("\"ExpiresAt\" IS NOT NULL");
                
                // Configure CategoryFilter as JSON array with value comparer
                var categoryFilterProperty = entity.Property(e => e.CategoryFilter)
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => v == null ? null : JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null))
                    .HasColumnType("jsonb");
                
                // Set value comparer for change tracking
                categoryFilterProperty.Metadata.SetValueComparer(
                    new ValueComparer<string[]?>(
                        // 相等比较
                        (c1, c2) => (c1 == null && c2 == null) ||
                                    (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                        // 哈希计算
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        // 快照克隆
                        c => c == null ? null : c.ToArray()));
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure Torrent.Cast with explicit JSON conversion and value comparer
            var castProperty = modelBuilder.Entity<Torrent>()
                .Property(t => t.Cast)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<CastMemberDto>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");
            
            // Set value comparer for change tracking
            castProperty.Metadata.SetValueComparer(
                new ValueComparer<List<CastMemberDto>?>(
                    // Equality comparison
                    (c1, c2) => (c1 == null && c2 == null) ||
                                (c1 != null && c2 != null &&
                                 c1.Count == c2.Count &&
                                 c1.SequenceEqual(c2, new CastMemberDtoComparer())),
                    // Hash code calculation
                    c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Name.GetHashCode())),
                    // Snapshot cloning
                    c => c == null ? null : c.Select(x => new CastMemberDto
                    {
                        Name = x.Name,
                        Character = x.Character,
                        ProfilePath = x.ProfilePath
                    }).ToList()));
        }
        
        // Custom comparer for CastMemberDto
        private class CastMemberDtoComparer : IEqualityComparer<CastMemberDto>
        {
            public bool Equals(CastMemberDto? x, CastMemberDto? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Name == y.Name &&
                       x.Character == y.Character &&
                       x.ProfilePath == y.ProfilePath;
            }

            public int GetHashCode(CastMemberDto obj)
            {
                return HashCode.Combine(obj.Name, obj.Character, obj.ProfilePath);
            }
        }
    }
}

