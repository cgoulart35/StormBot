using Newtonsoft.Json;

namespace StormBot.Models.CallOfDutyModels
{
    public class CallOfDutyLifetimeAllModel
    {
        public CallOfDutyLifetimeAllModel()
        {
            Properties = new CallOfDutyLifetimeAllPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyLifetimeAllPropertiesModel Properties { get; set; }
    }
}
