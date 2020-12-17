using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Players.Data
{
    public class WarzoneDataModel : ICallOfDutyDataModel
    {
        [JsonIgnore]
        public object ParticipatingAccountsFileLock { get; set; }

        [JsonIgnore]
        public object SavedPlayerDataFileLock { get; set; }

        [JsonIgnore]
        public string ParticipatingAccountsFileName { get { return "ModernWarfare"; } }

        [JsonIgnore]
        public string GameName { get { return "Warzone"; } }

        [JsonIgnore]
        public string GameAbbrevAPI { get { return "mw"; } }

        [JsonIgnore]
        public string ModeAbbrevAPI { get { return "wz"; } }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("lifetime")]
        public WarzoneLifetimeModel Lifetime { get; set; }

        [JsonProperty("weekly")]
        public ModernWarfareWeeklyModel Weekly { get; set; }
    }

    public class WarzoneLifetimeModel
    {

        [JsonProperty("mode")]
        public WarzoneLifetimeModeModel Mode { get; set; }
    }

    public class WarzoneLifetimeModeModel
    {
        [JsonProperty("br")]
        public WarzoneLifetimeBattleRoyalModel BattleRoyal { get; set; }
    }

    public class WarzoneLifetimeBattleRoyalModel
    {
        [JsonProperty("properties")]
        public WarzoneLifetimeBattleRoyalPropertiesModel Properties { get; set; }
    }

    public class WarzoneLifetimeBattleRoyalPropertiesModel
    {
        [JsonProperty("wins")]
        public double Wins { get; set; }
    }

    public class WarzoneWeeklyModel
    {
        [JsonProperty("all")]
        public WarzoneWeeklyAllModel All { get; set; }
    }

    public class WarzoneWeeklyAllModel
    {
        [JsonProperty("properties")]
        public WarzoneWeeklyAllPropertiesModel Properties { get; set; }
    }

    public class WarzoneWeeklyAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}