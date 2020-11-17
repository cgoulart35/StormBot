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
    }

    public class ModernWarfareLifetimeModel
    {
        [JsonProperty("all")]
        public ModernWarfareAllModel All { get; set; }

        [JsonProperty("mode")]
        public ModernWarfareModeModel Mode { get; set; }
    }

    public class ModernWarfareModeModel
    {
        [JsonProperty("br")]
        public ModernWarfareBattleRoyalModel BattleRoyal { get; set; }
    }

    public class ModernWarfareBattleRoyalModel
    {
        [JsonProperty("properties")]
        public ModernWarfareBattleRoyalPropertiesModel Properties { get; set; }
    }

    public class ModernWarfareBattleRoyalPropertiesModel
    {
        [JsonProperty("wins")]
        public double Wins { get; set; }
    }

    public class ModernWarfareAllModel
    {
        [JsonProperty("properties")]
        public ModernWarfareAllPropertiesModel Properties { get; set; }
    }

    public class ModernWarfareAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}