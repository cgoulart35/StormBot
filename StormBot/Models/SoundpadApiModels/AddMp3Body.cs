using Newtonsoft.Json;

namespace StormBot.Models.SoundpadApiModels
{
	public class AddMp3Body
	{
		[JsonProperty("source")]
		public string source;

		[JsonProperty("videoURL")]
		public string videoURL;

		[JsonProperty("soundName")]
		public string soundName;
	}
}