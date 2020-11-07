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
        private BaseService _baseService;
        private SoundpadService _soundpadService;
        private ModernWarfareService _modernWarfareService;
        private HelpService _helpService;
        private IServiceProvider _services;

        private static string DiscordToken;
        public static ulong SoundboardNotificationChannelID;
        public static string CategoryFoldersLocation;

        public static string Prefix = ".";

        private static bool isReady = false;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // get the discord bot token and soundboard notification channel ID from app.config
            ConfigureVariables();

            _client = new DiscordSocketClient();
            _client.Log += Log;

            // change boolean when client is ready
            _client.Ready += SetAsReady;

            await _client.LoginAsync(TokenType.Bot, DiscordToken);
            await _client.StartAsync();

            // wait until discord client is ready
            while (!isReady) { }

            // add singleton services
            ConfigureServices();

            _commandService.Log += Log;
            await _commandHandler.InitializeAsync();

            // ask the user if they want to start the soundpad service
            PromptUserForStartup(_soundpadService);

            // ask the user if they want to start the modern warfare service
            PromptUserForStartup(_modernWarfareService);

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // only services that were selected will be started
            _soundpadService.StartService();
            _modernWarfareService.StartService();

            // always start the help service 
            _helpService.DoStart = true;
            await _helpService.StartService();

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // block this task until the program is closed.
            await Task.Delay(-1);
        }

		private void ConfigureVariables()
        {
            try
            {
                string variables = "";
                if (ConfigurationManager.AppSettings["DiscordToken"] == "")
                {
                    variables += "DiscordToken ";
                }
                if (ConfigurationManager.AppSettings["SoundboardNotificationChannelID"] == "")
                {
                    variables += "SoundboardNotificationChannelID ";
                }
                if (ConfigurationManager.AppSettings["CategoryFoldersLocation"] == "")
                {
                    variables += "CategoryFoldersLocation ";
                }
                if (variables != "")
                {
                    throw new Exception("The following App.config variable(s) need to be configured: " + variables);
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
            CategoryFoldersLocation = ConfigurationManager.AppSettings["CategoryFoldersLocation"];
        }

        private void ConfigureServices()
        {
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<BaseService>()
                .AddSingleton<SoundpadService>()
                .AddSingleton<ModernWarfareService>()
                .AddSingleton<HelpService>()
                .BuildServiceProvider();

            _commandService = _services.GetRequiredService<CommandService>();
            _interactiveService = _services.GetRequiredService<InteractiveService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _baseService = _services.GetRequiredService<BaseService>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
            _modernWarfareService = _services.GetRequiredService<ModernWarfareService>();
            _helpService = _services.GetRequiredService<HelpService>();
        }

        private void PromptUserForStartup(BaseService service)
        {
            Console.WriteLine($"\nWould you like to start the {service.Name}? Please answer with 'y' or 'n'.");
            string answer = Console.ReadLine();

            if (answer.ToLower() == "yes" || answer.ToLower() == "y")
            {
                service.DoStart = true;
            }
            else if (answer.ToLower() == "no" || answer.ToLower() == "n")
            {
                service.DoStart = false;
            }
            else
            {
                Console.WriteLine("Input not valid: Please answer with 'y' or 'n'.");
                PromptUserForStartup(service);
            }
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
    }
}
