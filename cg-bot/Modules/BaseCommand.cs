using Discord.Addons.Interactive;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class BaseCommandModule : InteractiveBase<SocketCommandContext>
	{
		public string GetSingleArg(string[] args)
		{
			return args.Length != 0 ? string.Join(" ", args) : null;
		}
	}
}
