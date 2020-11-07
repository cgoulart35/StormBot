using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace cg_bot.Services
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

                int argPos = 0;
                if (message.HasStringPrefix(Program.Prefix, ref argPos))
                {
                    // execute command if one is found that matches
                    await _commandService.ExecuteAsync(context, argPos, _services);
                }
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                Console.WriteLine($"Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                return;
            }

            // if command not given correct amount of arguments, log that info to console and exit this method
            if (result.Error == CommandError.BadArgCount || result.Error == CommandError.ObjectNotFound)
            {
                Console.WriteLine($"Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                return;
            }

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.WriteLine($"Command [{command.Value.Name}] executed for -> [{context.User.Username}]");
                return;
            }

            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, {context.User.Username}... something went wrong -> [{result}]!");
        }
    }
}
