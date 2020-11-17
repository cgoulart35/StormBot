using System.Collections.Generic;
using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Players
{
    public class CallOfDutyAllPlayersModel<T>
	{
        public CallOfDutyAllPlayersModel(List<CallOfDutyPlayerModel<T>> players)
        {
            Players = players;
        }

        [JsonProperty("players")]
        public List<CallOfDutyPlayerModel<T>> Players { get; set; }
    }
}
