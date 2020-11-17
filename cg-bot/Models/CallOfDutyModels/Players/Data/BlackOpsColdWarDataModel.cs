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
    }

    public class BlackOpsColdWarLifetimeModel
    {
        [JsonProperty("all")]
        public BlackOpsColdWarAllModel All { get; set; }
    }

    public class BlackOpsColdWarAllModel
    {
        [JsonProperty("properties")]
        public BlackOpsColdWarAllPropertiesModel Properties { get; set; }
    }

    public class BlackOpsColdWarAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}