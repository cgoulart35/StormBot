using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class ModernWarfareCommands : BaseCommandModule
    {
        [Command("mwtest", RunMode = RunMode.Async)]
        public async Task MwtestCommand()
        {
	        await ReplyAsync("This is the mwtest command.");
        }
    }
}
