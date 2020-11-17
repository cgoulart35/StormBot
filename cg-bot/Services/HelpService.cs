using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Services
{
	class HelpService : BaseService
	{
		private readonly DiscordSocketClient _client;

		public HelpService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();

			Name = "Help Service";
			isServiceRunning = false;
		}
	}
}
