using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeModel
    {
        public CallOfDutyLifetimeModel()
        {
            All = new CallOfDutyLifetimeAllModel();
            Mode = new CallOfDutyLifetimeModeModel();
        }

        [JsonProperty("all")]
        public CallOfDutyLifetimeAllModel All { get; set; }

        [JsonProperty("mode")]
        public CallOfDutyLifetimeModeModel Mode { get; set; }
    }
}
