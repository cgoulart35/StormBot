using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Players.Data
{
    public class BlackOpsColdWarDataModel : ICallOfDutyDataModel
    {
        [JsonIgnore]
        public object ParticipatingAccountsFileLock { get; set; }

        [JsonIgnore]
        public object SavedPlayerDataFileLock { get; set; }

        [JsonIgnore]
        public string GameName { get { return "Black Ops Cold War"; } }

        [JsonIgnore]
        public string GameAbbrevAPI { get { return "cw"; } }

        [JsonIgnore]
        public string ModeAbbrevAPI { get { return "mp"; } }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("lifetime")]
        public BlackOpsColdWarLifetimeModel Lifetime { get; set; }

        [JsonProperty("weekly")]
        public BlackOpsColdWarWeeklyModel Weekly { get; set; }
    }

    public class BlackOpsColdWarLifetimeModel
    {
        [JsonProperty("all")]
        public BlackOpsColdWarLifetimeAllModel All { get; set; }
    }

    public class BlackOpsColdWarLifetimeAllModel
    {
        [JsonProperty("properties")]
        public BlackOpsColdWarLifetimeAllPropertiesModel Properties { get; set; }
    }

    public class BlackOpsColdWarLifetimeAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }

    public class BlackOpsColdWarWeeklyModel
    {
        [JsonProperty("all")]
        public BlackOpsColdWarWeeklyAllModel All { get; set; }
    }

    public class BlackOpsColdWarWeeklyAllModel
    {
        [JsonProperty("properties")]
        public BlackOpsColdWarWeeklyAllPropertiesModel Properties { get; set; }
    }

    public class BlackOpsColdWarWeeklyAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}