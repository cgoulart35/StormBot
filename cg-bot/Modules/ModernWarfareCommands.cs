using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class ModernWarfareCommands : InteractiveBase<SocketCommandContext>
	{
		public ModernWarfareCommands(IServiceProvider services)
		{

		}

        [Command("mwtest")]
        public async Task MwtestCommand()
        {
			await ReplyAsync("This is the mwtest command.");
        }
    }
}
