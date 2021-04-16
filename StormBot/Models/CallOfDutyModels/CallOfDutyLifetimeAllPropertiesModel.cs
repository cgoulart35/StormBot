using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}
