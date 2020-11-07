using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Services
{
	class ModernWarfareService : BaseService
    {
        private readonly DiscordSocketClient _client;

        public IMessageChannel _soundboardNotificationChannel;

        public ModernWarfareService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _soundboardNotificationChannel = _client.GetChannel(Program.SoundboardNotificationChannelID) as IMessageChannel;

            Name = "Modern Warfare Service";
            isServiceRunning = false;
        }
    }
}
