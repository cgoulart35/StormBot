using System;
using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Players
{
    public class CallOfDutyPlayerModel<T>
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("discordID")]
        public ulong DiscordID { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}