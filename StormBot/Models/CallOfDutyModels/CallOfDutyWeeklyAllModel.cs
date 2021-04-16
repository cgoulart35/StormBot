using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyWeeklyAllModel
    {
        public CallOfDutyWeeklyAllModel()
        {
            Properties = new CallOfDutyWeeklyAllPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyWeeklyAllPropertiesModel Properties { get; set; }
    }
}
