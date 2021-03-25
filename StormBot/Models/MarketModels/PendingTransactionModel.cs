
namespace StormBot.Models.MarketModels
{
	public class PendingTransactionModel
	{
		public ulong ServerID { get; set; }

		public ulong BuyerID { get; set; }

		public ulong OwnerID { get; set; }

		public string ItemName { get; set; }

		public int Status { get; set; }
	}
}
