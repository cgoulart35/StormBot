using Discord;
using Discord.WebSocket;
using SoundpadConnector;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using cg_bot.Database;

namespace cg_bot.Services
{
    class SoundpadService : BaseService
    {
        private readonly DiscordSocketClient _client;

        public Soundpad _soundpad;

        private bool displayedConnectingMessage;

        public bool isSoundpadRunning { get; set; }

        public SoundpadService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _db = services.GetRequiredService<CgBotContext>();

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
                _soundpad.AutoReconnect = true;
                _soundpad.StatusChanged += SoundpadOnStatusChangedAsync;

                await Task.Delay(5000);

                await _soundpad.ConnectAsync();
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
    }
}
