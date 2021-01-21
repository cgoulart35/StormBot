using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using cg_bot.Database.Entities;

namespace cg_bot.Database
{
	public class CgBotContext : DbContext
	{
		public virtual DbSet<ServersEntity> Servers { get; set; }

        public virtual DbSet<CallOfDutyPlayerDataEntity> CallOfDutyPlayerData { get; set; }

        public CgBotContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "cgbot.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            optionsBuilder.UseSqlite(connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CallOfDutyPlayerDataEntity>()
                .HasKey(p => new { p.ServerID, p.DiscordID, p.GameAbbrev, p.ModeAbbrev });
        }
    }
}
