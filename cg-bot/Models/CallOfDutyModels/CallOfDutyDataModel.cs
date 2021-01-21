using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels
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

    public class CallOfDutyLifetimeAllModel
    {
        public CallOfDutyLifetimeAllModel()
        {
            Properties = new CallOfDutyLifetimeAllPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyLifetimeAllPropertiesModel Properties { get; set; }
    }

    public class CallOfDutyLifetimeAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }

    public class CallOfDutyLifetimeModeModel
    {
        public CallOfDutyLifetimeModeModel()
        {
            BattleRoyal = new CallOfDutyLifetimeBattleRoyalModel();
        }

        [JsonProperty("br")]
        public CallOfDutyLifetimeBattleRoyalModel BattleRoyal { get; set; }
    }

    public class CallOfDutyLifetimeBattleRoyalModel
    {
        public CallOfDutyLifetimeBattleRoyalModel()
        {
            Properties = new CallOfDutyLifetimeBattleRoyalPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyLifetimeBattleRoyalPropertiesModel Properties { get; set; }
    }

    public class CallOfDutyLifetimeBattleRoyalPropertiesModel
    {
        [JsonProperty("wins")]
        public double Wins { get; set; }
    }

    public class CallOfDutyWeeklyModel
    {
        public CallOfDutyWeeklyModel()
        {
            All = new CallOfDutyWeeklyAllModel();
        }

        [JsonProperty("all")]
        public CallOfDutyWeeklyAllModel All { get; set; }
    }

    public class CallOfDutyWeeklyAllModel
    {
        public CallOfDutyWeeklyAllModel()
        {
            Properties = new CallOfDutyWeeklyAllPropertiesModel();
        }

        [JsonProperty("properties")]
        public CallOfDutyWeeklyAllPropertiesModel Properties { get; set; }
    }

    public class CallOfDutyWeeklyAllPropertiesModel
    {
        [JsonProperty("kills")]
        public double Kills { get; set; }
    }
}