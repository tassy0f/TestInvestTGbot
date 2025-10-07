using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Data.Entities;

namespace MyTestTelegramBot.Data.Repository
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<SteamHistoryDataItem> SteamHistoryData { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.ChatId)
                .IsUnique();
        }
    }
}
