using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Newtonsoft.Json;
using cg_bot.Services;
using cg_bot.Models;
using cg_bot.Models.CallOfDutyModels.Players.Data;

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
        private CallOfDutyService<ModernWarfareDataModel> _modernWarfareService;
        private CallOfDutyService<BlackOpsColdWarDataModel> _blackOpsColdWarService;
        private HelpService _helpService;
        private IServiceProvider _services;

        public static ConfigurationSettingsModel configurationSettingsModel;

        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string cgBotAppDataPath = Path.Combine(appDataPath, @"cg-bot\");
        public static string cgBotConfigSettingsPath = Path.Combine(cgBotAppDataPath, @"ConfigurationSettings.json");

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

            await _client.LoginAsync(TokenType.Bot, configurationSettingsModel.DiscordToken);
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

            // ask the user if they want to start the black ops cold war service
            PromptUserForStartup(_blackOpsColdWarService);

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // only services that were selected will be started
            _soundpadService.StartService();
            _modernWarfareService.StartService();
            _blackOpsColdWarService.StartService();

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
            // create app data directory if it doesn't exist
            if (!Directory.Exists(cgBotAppDataPath))
            {
                Console.WriteLine("The project folder %AppData%/Roaming/cg-bot does not exist.");
                Console.WriteLine("Creating the project folder %AppData%/Roaming/cg-bot...");
                Directory.CreateDirectory(cgBotAppDataPath);
                CreateNewConfigFile();
            }
            // create configuration file if it doesn't exist
            else if (!File.Exists(cgBotConfigSettingsPath))
            {
                Console.WriteLine("The file ConfigurationSettings.json does not exist.");
                CreateNewConfigFile();
            }
            // if it does exist, read in values; if it's corrupt re-create file
            else
            {
                bool createNewFile = true;
                try
                {
                    configurationSettingsModel = new ConfigurationSettingsModel();
                    configurationSettingsModel = JsonConvert.DeserializeObject<ConfigurationSettingsModel>(File.ReadAllText(cgBotConfigSettingsPath));

                    if (configurationSettingsModel.DiscordToken == null || configurationSettingsModel.DiscordToken == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the DiscordToken.");
                    }

                    if (configurationSettingsModel.CallOfDutyNotificationChannelID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the CallOfDutyNotificationChannelID.");
                    }

                    if (configurationSettingsModel.SoundboardNotificationChannelID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the SoundboardNotificationChannelID.");
                    }

                    if (configurationSettingsModel.ModernWarfareWarzoneWinsRoleID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the ModernWarfareWarzoneWinsRoleID.");
                    }

                    if (configurationSettingsModel.ModernWarfareKillsRoleID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the ModernWarfareKillsRoleID.");
                    }

                    if (configurationSettingsModel.BlackOpsColdWarKillsRoleID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the BlackOpsColdWarKillsRoleID.");
                    }

                    if (configurationSettingsModel.ActivisionEmail == null || configurationSettingsModel.ActivisionEmail == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the ActivisionEmail.");
                    }

                    if (configurationSettingsModel.ActivisionPassword == null || configurationSettingsModel.ActivisionPassword == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the ActivisionPassword.");
                    }

                    if (configurationSettingsModel.CategoryFoldersLocation == null || configurationSettingsModel.CategoryFoldersLocation == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the CategoryFoldersLocation.");
                    }

                    if (configurationSettingsModel.Prefix == null || configurationSettingsModel.Prefix == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the Prefix.");
                    }
                }
                catch(Exception error)
                {
                    if (createNewFile)
                    {
                        Console.WriteLine("The file ConfigurationSettings.json was corrupt.");
                        CreateNewConfigFile();
                    }

                    Console.WriteLine(error);
                    System.Threading.Thread.Sleep(10000);
                    Environment.Exit(1);
                }
            }
        }

        private void CreateNewConfigFile()
        {
            configurationSettingsModel = new ConfigurationSettingsModel();

            Console.WriteLine("Creating ConfigurationSettings.json file...");
            string json = JsonConvert.SerializeObject(configurationSettingsModel);
            File.WriteAllText(cgBotConfigSettingsPath, json);

            Console.WriteLine("Please fill out the configuration file completely, then re-run the application.");
            System.Threading.Thread.Sleep(10000);
            Environment.Exit(1);
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
                .AddSingleton<CallOfDutyService<ModernWarfareDataModel>>()
                .AddSingleton<CallOfDutyService<BlackOpsColdWarDataModel>>()
                .AddSingleton<HelpService>()
                .BuildServiceProvider();

            _commandService = _services.GetRequiredService<CommandService>();
            _interactiveService = _services.GetRequiredService<InteractiveService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _baseService = _services.GetRequiredService<BaseService>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
            _modernWarfareService = _services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
            _blackOpsColdWarService = _services.GetRequiredService<CallOfDutyService<BlackOpsColdWarDataModel>>();
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
