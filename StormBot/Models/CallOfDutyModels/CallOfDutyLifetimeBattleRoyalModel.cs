using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeBattleRoyalModel
    {
        public CallOfDutyLifetimeBattleRoyalModel()
        {
            Properties = new CallOfDutyLifetimeBattleRoyalPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyLifetimeBattleRoyalPropertiesModel Properties { get; set; }
    }
}
