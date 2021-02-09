using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StormBot.Database.Entities;

namespace StormBot.Database
{
	public class StormBotContext : DbContext
	{
		public virtual DbSet<ServersEntity> Servers { get; set; }

        public virtual DbSet<CallOfDutyPlayerDataEntity> CallOfDutyPlayerData { get; set; }

        public virtual DbSet<StormPlayerDataEntity> StormPlayerData { get; set; }

        public StormBotContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "StormBot.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            optionsBuilder.UseSqlite(connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CallOfDutyPlayerDataEntity>()
                .HasKey(p => new { p.ServerID, p.DiscordID, p.GameAbbrev, p.ModeAbbrev });

            modelBuilder.Entity<StormPlayerDataEntity>()
                .HasKey(p => new { p.ServerID, p.DiscordID });
        }
    }
}
