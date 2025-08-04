using Microsoft.EntityFrameworkCore;
using Sakura.PT.Entities;

namespace Sakura.PT.Data
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
                new StoreItem { Id = 1, ItemCode = Enums.StoreItemCode.UploadCredit10GB, Name = "10 GB Upload Credit", Description = "Add 10 GB to your total upload amount.", Price = 1000, IsAvailable = true },
                new StoreItem { Id = 2, ItemCode = Enums.StoreItemCode.UploadCredit50GB, Name = "50 GB Upload Credit", Description = "Add 50 GB to your total upload amount.", Price = 4500, IsAvailable = true },
                new StoreItem { Id = 3, ItemCode = Enums.StoreItemCode.InviteOne, Name = "1 Invite Code", Description = "Receive one invitation code to share with a friend.", Price = 5000, IsAvailable = true },
                new StoreItem { Id = 4, ItemCode = Enums.StoreItemCode.InviteFive, Name = "5 Invite Codes", Description = "Receive five invitation codes to share with your friends.", Price = 20000, IsAvailable = true },
                new StoreItem { Id = 5, ItemCode = Enums.StoreItemCode.DoubleUpload, Name = "Double Upload (24h)", Description = "All your uploads count double for 24 hours.", Price = 10000, IsAvailable = true },
                new StoreItem { Id = 6, ItemCode = Enums.StoreItemCode.NoHitAndRun, Name = "No Hit & Run (72h)", Description = "Exempt from Hit & Run rules for 72 hours.", Price = 15000, IsAvailable = true },
                new StoreItem { Id = 7, ItemCode = Enums.StoreItemCode.Badge, Name = "Sakura Coin Collector Badge", Description = "Show off your dedication to SakuraCoins!", Price = 25000, IsAvailable = true, BadgeId = 4 }
            );

            // Seed some badges
            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = 1, Name = "Early Supporter", Description = "Joined the site in its early days.", ImageUrl = "/images/badges/early_supporter.png", IsPurchasable = false },
                new Badge { Id = 2, Name = "Torrent Master", Description = "Uploaded 100+ torrents.", ImageUrl = "/images/badges/torrent_master.png", IsPurchasable = false },
                new Badge { Id = 3, Name = "Community Contributor", Description = "Active in forums and comments.", ImageUrl = "/images/badges/community_contributor.png", IsPurchasable = false },
                new Badge { Id = 4, Name = "Sakura Coin Collector", Description = "Purchased from the store.", ImageUrl = "/images/badges/coin_collector.png", IsPurchasable = true }
            );

            modelBuilder.Entity<UserBadge>()
                .HasIndex(ub => new { ub.UserId, ub.BadgeId })
                .IsUnique();
        }
    }
}
