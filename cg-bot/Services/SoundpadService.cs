using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;
using System;
using System.Threading.Tasks;

namespace cg_bot.Services
{
    class SoundpadService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        IMessageChannel _soundboardNotificationChannel;

        public Soundpad _soundpad;

        private bool isRunning;
        private bool displayedConnectingMessage;

        public SoundpadService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
        }

        public async Task StartService()
        {
            isRunning = false;
            displayedConnectingMessage = false;

            _soundboardNotificationChannel = _client.GetChannel(Program.SoundboardNotificationChannelID) as IMessageChannel;

            _soundpad = new Soundpad();
            _soundpad.AutoReconnect = true;
            _soundpad.StatusChanged += SoundpadOnStatusChangedAsync;

            if (isRunning)
            {
                Console.WriteLine("Soundpad service already running.");
            }
            else
            {
                Console.WriteLine("Starting Soundpad service.");

                isRunning = true;

                await _soundpad.ConnectAsync();
            }
        }

        private async void SoundpadOnStatusChangedAsync(object sender, EventArgs e)
        {
            if (_soundpad == null)
            {
                return;
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                string message = "SOUNDBOARD CONNECTED.";
                Console.WriteLine(message);
                await _soundboardNotificationChannel.SendMessageAsync("_**[    " + message + "    ]**_");
                displayedConnectingMessage = false;
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Disconnected && isRunning)
            {
                string message = "SOUNDBOARD DISCONNECTED.";
                Console.WriteLine(message);
                await _soundboardNotificationChannel.SendMessageAsync("_**[    " + message + "    ]**_");
                displayedConnectingMessage = false;
            }
            else if (_soundpad.ConnectionStatus == ConnectionStatus.Connecting && isRunning)
            {
                if (!displayedConnectingMessage)
                {
                    string message = "Trying to connect to in-game soundboard...";
                    Console.WriteLine(message);
                    displayedConnectingMessage = true;
                }
            }
        }
    }
}
