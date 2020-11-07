using System;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Services
{
	class HelpService : BaseService
	{
		private readonly DiscordSocketClient _client;

		public IMessageChannel _soundboardNotificationChannel;

		public HelpService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_soundboardNotificationChannel = _client.GetChannel(Program.SoundboardNotificationChannelID) as IMessageChannel;

			Name = "Help Service";
			isServiceRunning = false;
		}
	}
}
