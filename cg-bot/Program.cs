using System;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
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
        private CallOfDutyService<WarzoneDataModel> _warzoneService;
        private CallOfDutyService<BlackOpsColdWarDataModel> _blackOpsColdWarService;
        private AnnouncementsService _announcementsService;
        private HelpService _helpService;
        private IServiceProvider _services;

        public static ConfigurationSettingsModel configurationSettingsModel;

        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string cgBotAppDataPath = Path.Combine(appDataPath, @"cg-bot\");
        public static string cgBotConfigSettingsPath = Path.Combine(cgBotAppDataPath, @"ConfigurationSettings.json");

        private static bool isReady = false;

        /* TODO: replace for linux compatibility
        #region ON PROGRAM EXIT CODE
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler ConsoleApplicationClosed;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private bool CloseHandler(CtrlType sig)
        {
            // spacing for bot ouput visibility
            Console.WriteLine("");

            _soundpadService.StopService();
            _modernWarfareService.StopService();
            _warzoneService.StopService();
            _blackOpsColdWarService.StopService();
            _announcementsService.StopService();
            _helpService.StopService();

            System.Threading.Thread.Sleep(4000);
            Environment.Exit(1);

            return false;
        }
		#endregion
        */

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

            // ask the user if they want to start the modern warfare/warzone services
            PromptUserForStartup(_modernWarfareService);
            _warzoneService.DoStart = _modernWarfareService.DoStart;
            _warzoneService._dataModel.ParticipatingAccountsFileLock = _modernWarfareService._dataModel.ParticipatingAccountsFileLock;

            // ask the user if they want to start the black ops cold war service
            PromptUserForStartup(_blackOpsColdWarService);

            // spacing for bot ouput visibility
            Console.WriteLine("");

            // only services that were selected will be started
            _soundpadService.StartService();
            await _modernWarfareService.StartService();
            await _warzoneService.StartService();
            await _blackOpsColdWarService.StartService();


            // start the announcements service if the Modern Warfare/Warzone services or the Black Ops Cold War service is running
            _announcementsService.DoStart = false;
            if ((_modernWarfareService.isServiceRunning && _warzoneService.isServiceRunning) || _blackOpsColdWarService.isServiceRunning)
            {
                _announcementsService.DoStart = true;
            }
            await _announcementsService.StartService();

            // always start the help service
            _helpService.DoStart = true;
            await _helpService.StartService();

            /*
            // set stop service functions to be called on console application exit in close handler function
            ConsoleApplicationClosed += new EventHandler(CloseHandler);
            SetConsoleCtrlHandler(ConsoleApplicationClosed, true);
            */

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

                    if (configurationSettingsModel.WarzoneWinsRoleID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the WarzoneWinsRoleID.");
                    }

                    if (configurationSettingsModel.WarzoneKillsRoleID == 0)
                    {
                        createNewFile = false;
                        throw new Exception("Please fill out the WarzoneKillsRoleID.");
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
                .AddSingleton<AnnouncementsService>()
                .AddSingleton<SoundpadService>()
                .AddSingleton<CallOfDutyService<ModernWarfareDataModel>>()
                .AddSingleton<CallOfDutyService<WarzoneDataModel>>()
                .AddSingleton<CallOfDutyService<BlackOpsColdWarDataModel>>()
                .AddSingleton<HelpService>()
                .BuildServiceProvider();

            _commandService = _services.GetRequiredService<CommandService>();
            _interactiveService = _services.GetRequiredService<InteractiveService>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _baseService = _services.GetRequiredService<BaseService>();
            _soundpadService = _services.GetRequiredService<SoundpadService>();
            _announcementsService = _services.GetRequiredService<AnnouncementsService>();
            _modernWarfareService = _services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
            _warzoneService = _services.GetRequiredService<CallOfDutyService<WarzoneDataModel>>();
            _blackOpsColdWarService = _services.GetRequiredService<CallOfDutyService<BlackOpsColdWarDataModel>>();
            _helpService = _services.GetRequiredService<HelpService>();
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
