using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Players.Data
{
    public class ModernWarfareDataModel : ICallOfDutyDataModel
    {
        [JsonIgnore]
        public object ParticipatingAccountsFileLock { get; set; }

        [JsonIgnore]
        public object SavedPlayerDataFileLock { get; set; }

        [JsonIgnore]
        public string GameName { get { return "Modern Warfare"; } }

        [JsonIgnore]
        public string GameAbbrevAPI { get { return "mw"; } }

        [JsonIgnore]
        public string ModeAbbrevAPI { get { return "mp"; } }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("lifetime")]
        public ModernWarfareLifetimeModel Lifetime { get; set; }

        [JsonProperty("weekly")]
        public ModernWarfareWeeklyModel Weekly { get; set; }
    }

    public class ModernWarfareLifetimeModel
    {
        [JsonProperty("all")]
        public ModernWarfareLifetimeAllModel All { get; set; }

        [JsonProperty("mode")]
        public ModernWarfareLifetimeModeModel Mode { get; set; }
    }

    public class ModernWarfareLifetimeAllModel
    {
        [JsonProperty("properties")]
        public ModernWarfareLifetimeAllPropertiesModel Properties { get; set; }
    }

    public class ModernWarfareLifetimeAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }

    public class ModernWarfareLifetimeModeModel
    {
        [JsonProperty("br")]
        public ModernWarfareLifetimeBattleRoyalModel BattleRoyal { get; set; }
    }

    public class ModernWarfareLifetimeBattleRoyalModel
    {
        [JsonProperty("properties")]
        public ModernWarfareLifetimeBattleRoyalPropertiesModel Properties { get; set; }
    }

    public class ModernWarfareLifetimeBattleRoyalPropertiesModel
    {
        [JsonProperty("wins")]
        public double Wins { get; set; }
    }

    public class ModernWarfareWeeklyModel
    {
        [JsonProperty("all")]
        public ModernWarfareWeeklyAllModel All { get; set; }
    }

    public class ModernWarfareWeeklyAllModel
    {
        [JsonProperty("properties")]
        public ModernWarfareWeeklyAllPropertiesModel Properties { get; set; }
    }

    public class ModernWarfareWeeklyAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}