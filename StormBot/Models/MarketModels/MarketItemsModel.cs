using Newtonsoft.Json;
using System.Collections.Generic;

namespace StormBot.Models.MarketModels
{
	public class MarketItemsModel
	{
		[JsonProperty("items")]
		public List<MarketItemModel> Items { get; set; }
	}

	public class MarketItemModel
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("imageURL")]
		public string ImageURL { get; set; }

		[JsonProperty("hasBeenSold")]
		public bool HasBeenSold { get; set; }
	}
}
