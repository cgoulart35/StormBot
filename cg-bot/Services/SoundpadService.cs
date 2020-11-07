using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;
using System;
using System.Threading.Tasks;

namespace cg_bot.Services
{
    class SoundpadService : BaseService
    {
        private readonly DiscordSocketClient _client;

        public Soundpad _soundpad;

        public IMessageChannel _soundboardNotificationChannel;

        private bool displayedConnectingMessage;

        public bool isSoundpadRunning { get; set; }

        public SoundpadService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _soundboardNotificationChannel = _client.GetChannel(Program.SoundboardNotificationChannelID) as IMessageChannel;

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
                Console.WriteLine(logStamp + "Disabled.".PadLeft(45 - logStamp.Length));
            }
            else if (isServiceRunning)
            {
                Console.WriteLine(logStamp + "Service already running.".PadLeft(60 - logStamp.Length));
            }
            else
            {
                Console.WriteLine(logStamp + "Starting service.".PadLeft(53 - logStamp.Length));

                isServiceRunning = true;

                _soundpad = new Soundpad();
                _soundpad.AutoReconnect = true;
                _soundpad.StatusChanged += SoundpadOnStatusChangedAsync;
                await _soundpad.ConnectAsync();
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
                Console.WriteLine(logStamp + message.PadLeft(57 - logStamp.Length));
                await _soundboardNotificationChannel.SendMessageAsync("_**[    " + message + "    ]**_");
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Disconnected && isSoundpadRunning)
            {
                displayedConnectingMessage = false;
                string message = "SOUNDBOARD DISCONNECTED.";
                Console.WriteLine(logStamp + message.PadLeft(60 - logStamp.Length));
                await _soundboardNotificationChannel.SendMessageAsync("_**[    " + message + "    ]**_");
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Connecting && isSoundpadRunning)
            {
                if (!displayedConnectingMessage)
                {
                    displayedConnectingMessage = true;
                    isSoundpadRunning = false;
                    string message = "Listening for the soundboard application...";
                    Console.WriteLine(logStamp + message.PadLeft(79 - logStamp.Length));
                }
            }
        }
    }
}
