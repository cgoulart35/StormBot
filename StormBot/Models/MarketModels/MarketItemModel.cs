using Newtonsoft.Json;

namespace StormBot.Models.MarketModels
{
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
