using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace StormBot.Services
{
    class CommandHandler
    {
        private readonly CommandService _commandService;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _commandService = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _services = services;

            // take action when we execute a command
            _commandService.CommandExecuted += CommandExecutedAsync;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
			var message = arg as SocketUserMessage;

            if (message != null)
            {
                var context = new SocketCommandContext(_client, message);

                // if another bot sent the message, then return
                if (message.Author.IsBot) return;

                if (!context.IsPrivate)
                {
                    int argPos = 0;
                    string serverPrefix = BaseService.GetServerPrefix(context.Guild.Id);

                    if (message.HasStringPrefix(serverPrefix, ref argPos))
                    {
                        // execute command if one is found that matches
                        await _commandService.ExecuteAsync(context, argPos, _services);
                    }
                }
                else
                {
                    int argPos = 0;
                    if (message.HasStringPrefix(Program.configurationSettingsModel.PrivateMessagePrefix, ref argPos))
                    {
                        // execute command if one is found that matches
                        await _commandService.ExecuteAsync(context, argPos, _services);
                    }
                }
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            // if command not given correct amount of arguments, log that info to console and exit this method
            // if command args using backslash and not escaped correctly
            if (!command.IsSpecified ||
                result.Error == CommandError.BadArgCount || result.Error == CommandError.ObjectNotFound ||
                result.Error == CommandError.ParseFailed)
            {
                Console.WriteLine($"Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                return;
            }

            // if command was ran by user who was not an administrator when administrator privileges are needed
            if (result.Error == CommandError.UnmetPrecondition)
            {
                Console.WriteLine($"Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                await context.Channel.SendMessageAsync($"Sorry, {context.User.Username} only Administrators can run this command.");
                return;
            }

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.WriteLine($"Command [{command.Value.Name}] executed for -> [{context.User.Username}]");
                return;
            }

            // TODO: remove after debugging purposes fulfilled; any existing bugs?
            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, {context.User.Username}... something went wrong -> [{result}]!");
        }
    }
}
