using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using cg_bot.Services;

namespace cg_bot
{
    public class Program
    { 
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private InteractiveService _interactiveService;
        private CommandHandler _commandHandler;
        private SoundpadService _soundpadService;
        private IServiceProvider _services;

        private static string DiscordToken;
        public static ulong SoundboardNotificationChannelID;

        private static bool isReady = false;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // get the discord bot token and soundboard notification channel ID from app.config
            ConfigureVariables();

            // add singleton services
            ConfigureServices();

            _client.Log += Log;
            _commandService.Log += Log;

            // change boolean when ready
            _client.Ready += SetAsReady;

            await _client.LoginAsync(TokenType.Bot, DiscordToken);
            await _client.StartAsync();
            
            await _commandHandler.InitializeAsync();

            // when ready, start the soundpad service
            while (!isReady) { }
            await _soundpadService.StartService();

            // block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

		private async Task SetAsReady()
		{
			isReady = true;
		}

		private void ConfigureVariables()
        {
            try
            {
                if (ConfigurationManager.AppSettings["DiscordToken"] == "" && ConfigurationManager.AppSettings["SoundboardNotificationChannelID"] == "")
                {
                    throw new Exception("App.config variables need to be configured: DiscordToken, SoundboardNotificationChannelID");
                }
                if (ConfigurationManager.AppSettings["DiscordToken"] == "")
                {
                    throw new Exception("App.config variable needs to be configured: DiscordToken");
                }
                if (ConfigurationManager.AppSettings["SoundboardNotificationChannelID"] == "")
                {
                    throw new Exception("App.config variable needs to be configured: SoundboardNotificationChannelID");
                }
            }

            // if the user has not configured the discord bot token and soundboard notification channel ID from app.config, show error for 10 seconds
            catch (Exception error)
            {
                Console.WriteLine(error);
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(1);
            }

            DiscordToken = ConfigurationManager.AppSettings["DiscordToken"];
            SoundboardNotificationChannelID = ulong.Parse(ConfigurationManager.AppSettings["SoundboardNotificationChannelID"]);
        }

        private void ConfigureServices()
        {
            _services = new ServiceCollection()
               .AddSingleton<DiscordSocketClient>()
               .AddSingleton<CommandService>()
               .AddSingleton<InteractiveService>()
               .AddSingleton<CommandHandler>()
               .AddSingleton<SoundpadService>()
               .BuildServiceProvider();

            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commandService = _services.GetRequiredService<CommandService>();
            _interactiveService = _services.GetRequiredService<InteractiveService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
        }
    }
}
