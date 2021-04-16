using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyWeeklyAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}
