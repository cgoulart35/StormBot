using System.ComponentModel.DataAnnotations.Schema;

namespace StormBot.Database.Entities
{
	[Table("StormPlayerDataEntity")]
	public class StormPlayerDataEntity
	{
		[ForeignKey("ServersEntity")]
		public ulong ServerID { get; set; }

		public ulong DiscordID { get; set; }

		public double Wallet { get; set; }
	}
}
