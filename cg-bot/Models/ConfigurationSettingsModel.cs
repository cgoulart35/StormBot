using Newtonsoft.Json;

namespace cg_bot.Entities
{
	public class ConfigurationSettingsModel
	{
		[JsonProperty("RemoteBootMode")]
		public bool RemoteBootMode { get; set; }

		[JsonProperty("DiscordToken")]
		public string DiscordToken { get; set; }

		[JsonProperty("PrivateMessagePrefix")]
		public string PrivateMessagePrefix { get; set; }

		[JsonProperty("ActivisionEmail")]
		public string ActivisionEmail { get; set; }

		[JsonProperty("ActivisionPassword")]
		public string ActivisionPassword { get; set; }

		[JsonProperty("CategoryFoldersLocation")]
		public string CategoryFoldersLocation { get; set; }
	}
}