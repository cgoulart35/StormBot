using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using cg_bot.Services;

namespace cg_bot
{
    public class Program
    { 
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private CommandHandler _commandHandler;
        private SoundpadService _soundpadService;
        private IServiceProvider _services;

        public static string DiscordToken;
        public static ulong SoundboardNotificationChannelID;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            ConfigureVariables();
            ConfigureServices();

            _client.Log += Log;
            _commandService.Log += Log;

            _client.Ready += _soundpadService.StartService;

            await _client.LoginAsync(TokenType.Bot, DiscordToken);
            await _client.StartAsync();
            
            await _commandHandler.InitializeAsync();

            // block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
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
            catch(Exception error)
            {
                Console.WriteLine(error);
                System.Threading.Thread.Sleep(5000);
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
               .AddSingleton<CommandHandler>()
               .AddSingleton<SoundpadService>()
               .BuildServiceProvider();

            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commandService = _services.GetRequiredService<CommandService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
        }
    }
}
