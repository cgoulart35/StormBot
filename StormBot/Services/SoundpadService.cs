using Discord;
using Discord.WebSocket;
using Discord.Commands;
using SoundpadConnector;
using SoundpadConnector.Response;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using StormBot.Database;
using StormBot.Models.SoundpadApiModels;

namespace StormBot.Services
{
    class SoundpadService : BaseService
    {
        private readonly DiscordSocketClient _client;

        public Soundpad _soundpad;

        private bool displayedConnectingMessage;

        public bool isSoundpadRunning { get; set; }

        public SoundpadService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();

            Name = "Soundpad Service";
            isServiceRunning = false;
            isSoundpadRunning = false;
            displayedConnectingMessage = false;
        }

        public override async Task StartService()
        {
            string logStamp = GetLogStamp();

            if (!DoStart)
            {
                Console.WriteLine(logStamp + "Disabled.".PadLeft(60 - logStamp.Length));
            }
            else if (isServiceRunning)
            {
                Console.WriteLine(logStamp + "Service already running.".PadLeft(75 - logStamp.Length));
            }
            else
            {
                Console.WriteLine(logStamp + "Starting service.".PadLeft(68 - logStamp.Length));

                isServiceRunning = true;

                _soundpad = new Soundpad();
                _soundpad.StatusChanged += SoundpadOnStatusChangedAsync;

                await Task.Delay(5000);

                // skip these steps if bot is using RemoteBootMode and StormBotSoundpadApi
                if (!Program.configurationSettingsModel.RemoteBootMode)
                {
                    _soundpad.AutoReconnect = true;
                    await _soundpad.ConnectAsync();
                }
                // get connection status from the API every second and update it here and call SoundpadOnStatusChangedAsync if it has changed
                else
                {
                    PollSoundpadStatus();
                }
            }
        }

        public override async Task StopService()
        {
            string logStamp = GetLogStamp();

            if (isServiceRunning)
            {
                Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

                isServiceRunning = false;

                if (isSoundpadRunning)
                {
                    string message = "SOUNDBOARD DISCONNECTED.";
                    Console.WriteLine(logStamp + message.PadLeft(75 - logStamp.Length));

                    var channels = await GetAllServerSoundpadChannels();
                    
                    if (channels.Count() != 0)
                        channels.ToList().ForEach(channel => channel.SendMessageAsync("_**[    " + message + "    ]**_"));
                }
            }
        }

        private async void SoundpadOnStatusChangedAsync(object sender, EventArgs e)
        {
            string logStamp = GetLogStamp();

            if (_soundpad == null)
            {
                return;
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                displayedConnectingMessage = false;
                isSoundpadRunning = true;

                string message = "SOUNDBOARD CONNECTED.";
                Console.WriteLine(logStamp + message.PadLeft(72 - logStamp.Length));

                var channels = await GetAllServerSoundpadChannels();

                if (channels.Count() != 0)
                    channels.ToList().ForEach(channel => channel.SendMessageAsync("_**[    " + message + "    ]**_"));
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Disconnected && isSoundpadRunning)
            {
                displayedConnectingMessage = false;

                string message = "SOUNDBOARD DISCONNECTED.";
                Console.WriteLine(logStamp + message.PadLeft(75 - logStamp.Length));

                var channels = await GetAllServerSoundpadChannels();

                if (channels.Count() != 0)
                    channels.ToList().ForEach(channel => channel.SendMessageAsync("_**[    " + message + "    ]**_"));
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Connecting && isSoundpadRunning)
            {
                if (!displayedConnectingMessage)
                {
                    displayedConnectingMessage = true;
                    isSoundpadRunning = false;

                    string message = "Listening for the soundboard application...";
                    Console.WriteLine(logStamp + message.PadLeft(94 - logStamp.Length));
                }
            }
        }

        #region API
        private async Task PollSoundpadStatus()
        {
            while (isServiceRunning)
            {
                ConnectionStatus connectionStatus = GetConnectionStatusAPI();

                if (connectionStatus != _soundpad.ConnectionStatus)
                {
                    if (connectionStatus == ConnectionStatus.Connected)
                    {
                        _soundpad.ConnectionStatus = ConnectionStatus.Connected;
                        SoundpadOnStatusChangedAsync(this, EventArgs.Empty);
                    }
                    else if (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Connecting)
                    {
                        _soundpad.ConnectionStatus = ConnectionStatus.Disconnected;
                        SoundpadOnStatusChangedAsync(this, EventArgs.Empty);

                        _soundpad.ConnectionStatus = ConnectionStatus.Connecting;
                        SoundpadOnStatusChangedAsync(this, EventArgs.Empty);
                    }
                }

                await Task.Delay(1000);
            }
        }

        public ConnectionStatus GetConnectionStatusAPI()
        {
            // if the machine hosting the API is offline, catch the exception otherwise polling will stop working
            try
            {
                RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "status");
                RestRequest request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);

                return JsonConvert.DeserializeObject<ConnectionStatus>(response.Content);
            }
            catch
            {
                return ConnectionStatus.Disconnected;
            }
        }

        public CategoryResponse GetCategoryAPI(int index)
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "category/" + index);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<CategoryResponse>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return null;
            }
        }

        public CategoryListResponse GetCategoriesAPI()
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "categories");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<CategoryListResponse>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return null;
            }
        }

        public ApiValidationResponse SaveMP3API(string source, string videoURL, string soundName)
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "addMp3");
            RestRequest request = new RestRequest(Method.POST);

            AddMp3Body body = new AddMp3Body
            {
                source = source,
                videoURL = videoURL,
                soundName = soundName
            };

            request.AddJsonBody(body);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new ApiValidationResponse
                {
                    Successful = JsonConvert.DeserializeObject<bool>(response.Content),
                    Information = "Success"
                };
            }
            else
            {
                Console.WriteLine(response.Content);
                return new ApiValidationResponse
                {
                    Successful = false,
                    Information = response.Content.Trim(' ', '\t', '\n', '\v', '\f', '\r', '"')
            };
            }
        }

        public bool AddSoundAPI(string path, int index, int categoryIndex)
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "addSound");
            RestRequest request = new RestRequest(Method.POST);

            AddSoundBody body = new AddSoundBody
            {
                path = path,
                index = index,
                categoryIndex = categoryIndex
            };

            request.AddJsonBody(body);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }

        public bool SelectIndexAPI(int index)
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "select/" + index);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }

        public bool RemoveSelectedEntriesAPI()
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "removeSelected");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }

        public bool PlaySoundAPI(int index)
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "play/" + index);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }

        public bool PauseSoundAPI()
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "pause");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }

        public bool StopSoundAPI()
        {
            RestClient client = new RestClient(Program.configurationSettingsModel.StormBotSoundpadApiHostname + "stop");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content);
            }
            else
            {
                Console.WriteLine(response.Content);
                return false;
            }
        }
        #endregion

        #region QUERIES
        private async Task<IEnumerable<IMessageChannel>> GetAllServerSoundpadChannels()
        {
            var channelIds = await _db.Servers
                .AsQueryable()
                .Where(s => s.SoundboardNotificationChannelID != 0 && s.AllowServerPermissionSoundpadCommands && s.ToggleSoundpadCommands)
                .Select(s => s.SoundboardNotificationChannelID)
                .AsAsyncEnumerable()
                .ToListAsync();

            return channelIds.Select(channelId => _client.GetChannel(channelId) as IMessageChannel);
        }

        public async Task<bool> GetServerToggleSoundpadCommands(SocketCommandContext context)
        {
            if (!context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == context.Guild.Id)
                .Select(s => s.ToggleSoundpadCommands)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerAllowServerPermissionSoundpadCommands(SocketCommandContext context)
        {
            if (!context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == context.Guild.Id)
                .Select(s => s.AllowServerPermissionSoundpadCommands)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }
		#endregion
	}
}
