using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyPlayerModel
    {
        public CallOfDutyPlayerModel()
        {
            Data = new CallOfDutyDataModel();
        }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public CallOfDutyDataModel Data { get; set; }

        [JsonProperty("discordID")]
        public ulong DiscordID { get; set; }
    }
}