using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Newtonsoft.Json;
using StormBot.Services;
using StormBot.Entities;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot
{
	public class Program
    { 
        private DiscordSocketClient _client;
        private StormBotContext _db;
        private CommandService _commandService;
        private InteractiveService _interactiveService;
        private CommandHandler _commandHandler;
        private BaseService _baseService;
        private SoundpadService _soundpadService;
        private CallOfDutyService _callOfDutyService;
        private AnnouncementsService _announcementsService;
        private IServiceProvider _services;

        public static ConfigurationSettingsModel configurationSettingsModel;

        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string StormBotAppDataPath = Path.Combine(appDataPath, @"StormBot\");
        public static string StormBotConfigSettingsPath = Path.Combine(StormBotAppDataPath, @"ConfigurationSettings.json");

        private static bool isReady = false;

		public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // get the config file settings
            ConfigureVariables();

            _client = new DiscordSocketClient();
            _client.Log += Log;

            // change boolean when client is ready
            _client.Ready += SetAsReady;

            // add server data with default values in database when added to server
            _client.JoinedGuild += AddServerToDatabase;

            // remove server data in database when added to server
            _client.LeftGuild += RemoveServerFromDatabase;

            await _client.LoginAsync(TokenType.Bot, configurationSettingsModel.DiscordToken);
            await _client.StartAsync();

            // wait until discord client is ready
            while (!isReady) { }

            await _client.SetGameAsync(".help", null, ActivityType.Listening);

            // add singleton services
            ConfigureServices();

            _commandService.Log += Log;
            await _commandHandler.InitializeAsync();

            if (!configurationSettingsModel.RemoteBootMode)
            {
                // ask the user if they want to start the soundpad service
                PromptUserForStartup(_soundpadService);

                // ask the user if they want to start the call of duty services
                PromptUserForStartup(_callOfDutyService);

                // ask the user what call of duty service components to enable if the service is enabled
                if (_callOfDutyService.DoStart == true)
                {        
                    PromptUserForStartup(_callOfDutyService.ModernWarfareComponent);
                    PromptUserForStartup(_callOfDutyService.WarzoneComponent);
                    PromptUserForStartup(_callOfDutyService.BlackOpsColdWarComponent);
                }
            }
            // start all services except soundpad service if in remote boot mode
            else
            {
                _soundpadService.DoStart = false;
                _callOfDutyService.DoStart = true;
                _callOfDutyService.ModernWarfareComponent.DoStart = true;
                _callOfDutyService.WarzoneComponent.DoStart = true;
                _callOfDutyService.BlackOpsColdWarComponent.DoStart = true;
            }

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // only services that were selected will be started
            _soundpadService.StartService();
            await _callOfDutyService.StartService();

            // start the announcements service if a call f duty service is running
            _announcementsService.DoStart = false;
            if (_callOfDutyService.isServiceRunning)
            {
                _announcementsService.DoStart = true;
            }
            await _announcementsService.StartService();

            // set stop service functions to be called on console application exit in close handler function
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ShutdownHandler);

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // block this task until the program is closed.
            await Task.Delay(-1);
        }

		private void ConfigureVariables()
        {
            // create app data directory if it doesn't exist
            if (!Directory.Exists(StormBotAppDataPath))
            {
                Console.WriteLine("The project folder %AppData%/Roaming/StormBot does not exist.");
                Console.WriteLine("Creating the project folder %AppData%/Roaming/StormBot...");
                Directory.CreateDirectory(StormBotAppDataPath);
                CreateNewConfigFile();
            }
            // create configuration file if it doesn't exist
            else if (!File.Exists(StormBotConfigSettingsPath))
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
                    configurationSettingsModel = JsonConvert.DeserializeObject<ConfigurationSettingsModel>(File.ReadAllText(StormBotConfigSettingsPath));

                    if (configurationSettingsModel.DiscordToken == null || configurationSettingsModel.DiscordToken == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the DiscordToken.");
                    }

                    if (configurationSettingsModel.PrivateMessagePrefix == null || configurationSettingsModel.PrivateMessagePrefix == "")
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the PrivateMessagePrefix.");
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
            File.WriteAllText(StormBotConfigSettingsPath, json);

            Console.WriteLine("Please fill out the configuration file completely, then re-run the application.");
            System.Threading.Thread.Sleep(10000);
            Environment.Exit(1);
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

        private async Task AddServerToDatabase(SocketGuild guild)
        {
            ServersEntity newServerData = new ServersEntity()
            {
                ServerID = guild.Id,
                ServerName = guild.Name,
                PrefixUsed = configurationSettingsModel.PrivateMessagePrefix,
                AllowServerPermissionBlackOpsColdWarTracking = true,
                ToggleBlackOpsColdWarTracking = false,
                AllowServerPermissionModernWarfareTracking = true,
                ToggleModernWarfareTracking = false,
                AllowServerPermissionWarzoneTracking = true,
                ToggleWarzoneTracking = false,
                AllowServerPermissionSoundpadCommands = true,
                ToggleSoundpadCommands = false,
                CallOfDutyNotificationChannelID = 0,
                SoundboardNotificationChannelID = 0,
                AdminRoleID = 0,
                WarzoneWinsRoleID = 0,
                WarzoneKillsRoleID = 0,
                ModernWarfareKillsRoleID = 0,
                BlackOpsColdWarKillsRoleID = 0
            };

            _db.Servers.Add(newServerData);
            await _db.SaveChangesAsync();
        }

        private async Task RemoveServerFromDatabase(SocketGuild guild)
        {
            var s = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == guild.Id)
                .AsAsyncEnumerable()
                .ToListAsync();

            var c = await _db.CallOfDutyPlayerData
                .AsQueryable()
                .Where(c => c.ServerID == guild.Id)
                .AsAsyncEnumerable()
                .ToListAsync();

            _db.RemoveRange(s);
            _db.RemoveRange(c);
            await _db.SaveChangesAsync();
        }

        private void ConfigureServices()
        {
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddDbContext<StormBotContext>()
                .AddSingleton<CommandService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<BaseService>()
                .AddSingleton<SoundpadService>()
                .AddSingleton<CallOfDutyService>()
                .AddSingleton<AnnouncementsService>()
                .BuildServiceProvider();

            _db = _services.GetRequiredService<StormBotContext>();
            _commandService = _services.GetRequiredService<CommandService>();
            _interactiveService = _services.GetRequiredService<InteractiveService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _baseService = _services.GetRequiredService<BaseService>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
            _callOfDutyService = _services.GetRequiredService<CallOfDutyService>();
            _announcementsService = _services.GetRequiredService<AnnouncementsService>();
        }

        private void PromptUserForStartup(BaseService service)
        {
            Console.WriteLine($"\nWould you like to start the {service.Name}? Please answer with 'y' or 'n'.");
            string answer = Console.ReadLine();

            if (answer != null && (answer.ToLower() == "yes" || answer.ToLower() == "y"))
            {
                service.DoStart = true;
            }
            else if (answer != null && (answer.ToLower() == "no" || answer.ToLower() == "n"))
            {
                service.DoStart = false;
            }
            else
            {
                Console.WriteLine("Input not valid: Please answer with 'y' or 'n'.");
                PromptUserForStartup(service);
            }
        }

        private void ShutdownHandler(object sender, ConsoleCancelEventArgs args)
        {
            // spacing for bot ouput visibility
            Console.WriteLine("");

            _soundpadService.StopService();
            _callOfDutyService.StopService();
            _announcementsService.StopService();

            System.Threading.Thread.Sleep(8000);
            Environment.Exit(1);
        }
    }
}