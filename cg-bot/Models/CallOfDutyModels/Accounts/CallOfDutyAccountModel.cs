using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Accounts
{
    public class CallOfDutyAccountModel
    {
        [JsonProperty("discordID")]
        public ulong DiscordID { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }
    }
}
