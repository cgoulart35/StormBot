using Newtonsoft.Json;
using System.Collections.Generic;

namespace StormBot.Models.MarketModels
{
	public class MarketItemsModel
	{
		[JsonProperty("items")]
		public List<MarketItemModel> Items { get; set; }
	}
}
