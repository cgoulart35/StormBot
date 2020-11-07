using System;
using System.Threading.Tasks;
using cg_bot.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Modules
{
    public class ModernWarfareCommands : BaseCommand
    {
        private ModernWarfareService _service;

        public ModernWarfareCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<ModernWarfareService>();
        }

        [Command("mwtest", RunMode = RunMode.Async)]
        public async Task MwtestCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mwtest"))
            {
                await ReplyAsync("This is the mwtest command.");
            }
        }

        // FEATURE: Implement MW service commands
    }
}
