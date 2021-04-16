using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeBattleRoyalPropertiesModel
    {
        [JsonProperty("wins")]
        public double Wins { get; set; }
    }
}
