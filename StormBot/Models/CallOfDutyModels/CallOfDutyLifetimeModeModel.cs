using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeModeModel
    {
        public CallOfDutyLifetimeModeModel()
        {
            BattleRoyal = new CallOfDutyLifetimeBattleRoyalModel();
        }

        [JsonProperty("br")]
        public CallOfDutyLifetimeBattleRoyalModel BattleRoyal { get; set; }
    }
}
