using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyWeeklyModel
    {
        public CallOfDutyWeeklyModel()
        {
            All = new CallOfDutyWeeklyAllModel();
        }

        [JsonProperty("all")]
        public CallOfDutyWeeklyAllModel All { get; set; }
    }
}
