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
        }
    }
}
