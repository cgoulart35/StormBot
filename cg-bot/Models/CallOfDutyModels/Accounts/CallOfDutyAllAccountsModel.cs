using System.Collections.Generic;
using Newtonsoft.Json;

namespace cg_bot.Models.CallOfDutyModels.Accounts
{
	public class CallOfDutyAllAccountsModel
	{
		[JsonProperty("accounts")]
		public List<CallOfDutyAccountModel> Accounts { get; set; }
	}
}
