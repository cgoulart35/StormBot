using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class ModernWarfareCommands : BaseCommandModule
    {
        public ModernWarfareCommands(IServiceProvider services)
        {

        }

        [Command("mwtest", RunMode = RunMode.Async)]
        public async Task MwtestCommand()
        {
	        await ReplyAsync("This is the mwtest command.");
        }
    }
}
