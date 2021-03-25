using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StormBot.Database.Entities
{
	[Table("MarketPlayerDataEntity")]
	public class MarketPlayerDataEntity
	{
		[Key]
		public ulong DiscordID { get; set; }

		public string MarketItemsJSON { get; set; }
	}
}
