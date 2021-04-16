using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyDataModel
    {
        public CallOfDutyDataModel()
        {
            Lifetime = new CallOfDutyLifetimeModel();
            Weekly = new CallOfDutyWeeklyModel();
        }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("lifetime")]
        public CallOfDutyLifetimeModel Lifetime { get; set; }

        [JsonProperty("weekly")]
        public CallOfDutyWeeklyModel Weekly { get; set; }
    }
}