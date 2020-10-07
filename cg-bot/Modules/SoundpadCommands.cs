using System;
using System.Threading.Tasks;
using cg_bot.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;

namespace cg_bot.Modules
{
    public class SoundpadCommands : ModuleBase<SocketCommandContext>
    {
        private readonly SoundpadService _soundpadService;
        private readonly IServiceProvider _services;

        public Soundpad _soundpad;

        public SoundpadCommands(IServiceProvider services)
        {
            _soundpadService = services.GetRequiredService<SoundpadService>();
            _services = services;

            _soundpad = _soundpadService._soundpad;
        }

        [Command("test")]
        public async Task TestCommand()
        {
            await ReplyAsync("Test Command.");
        }
    }
}
