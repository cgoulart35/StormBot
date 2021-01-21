using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace cg_bot.Database.Entities
{
	[Table("CallOfDutyPlayerDataEntity")]
	public class CallOfDutyPlayerDataEntity
	{
		[ForeignKey("ServersEntity")]
		public ulong ServerID { get; set; }

		public ulong DiscordID { get; set; }

		public string GameAbbrev { get; set; }

		public string ModeAbbrev { get; set; }

		public string Username { get; set; }

		public string Tag { get; set; }

		public string Platform { get; set; }

		public double TotalKills { get; set; }

		public double TotalWins { get; set; }

		public double WeeklyKills { get; set; }

		public DateTime Date { get; set; }
	}
}
