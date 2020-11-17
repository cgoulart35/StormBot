using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cg_bot.Models
{
	public class ConfigurationSettingsModel
	{
		[JsonProperty("DiscordToken")]
		public string DiscordToken { get; set; }

		[JsonProperty("CallOfDutyNotificationChannelID")]
		public ulong CallOfDutyNotificationChannelID { get; set; }

		[JsonProperty("SoundboardNotificationChannelID")]
		public ulong SoundboardNotificationChannelID { get; set; }

		[JsonProperty("ModernWarfareWarzoneWinsRoleID")]
		public ulong ModernWarfareWarzoneWinsRoleID { get; set; }

		[JsonProperty("ModernWarfareKillsRoleID")]
		public ulong ModernWarfareKillsRoleID { get; set; }

		[JsonProperty("BlackOpsColdWarKillsRoleID")]
		public ulong BlackOpsColdWarKillsRoleID { get; set; }

		[JsonProperty("ActivisionEmail")]
		public string ActivisionEmail { get; set; }

		[JsonProperty("ActivisionPassword")]
		public string ActivisionPassword { get; set; }

		[JsonProperty("CategoryFoldersLocation")]
		public string CategoryFoldersLocation { get; set; }

		[JsonProperty("Prefix")]
		public string Prefix { get; set; }
	}
}