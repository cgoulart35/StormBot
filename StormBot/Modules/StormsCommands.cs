using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using StormBot.Services;
using StormBot.Database.Entities;

namespace StormBot.Modules
{
	public class StormsCommands : BaseCommand
    {
        private StormsService _service;
        private AnnouncementsService _announcementsService;

        static bool handlersSet = false;

        public StormsCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<StormsService>();
            _announcementsService = services.GetRequiredService<AnnouncementsService>();

            if (!handlersSet)
            {
                _announcementsService.RandomStormAnnouncement += _service.HandleIncomingStorm;

                handlersSet = true;
            }
        }

        #region COMMANDS
        [Command("umbrella", RunMode = RunMode.Async)]
        public async Task RaiseUmbrellaCommand()
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "umbrella"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        await _service.TryToUpdateOngoingStorm(serverId, discordId, channelId, 1);
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("guess", RunMode = RunMode.Async)]
        public async Task GuessCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "guess"))
                    {
                        if (args.Length == 1)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong channelId = Context.Channel.Id;
                            ulong discordId = Context.User.Id;

                            string guessStr = GetSingleArg(args);

                            // if arg is a number
                            if (int.TryParse(guessStr, out int guess))
                            {
                                // if number is valid
                                if (guess >= 1 && guess <= 200)
                                {
                                    await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    await _service.TryToUpdateOngoingStorm(serverId, discordId, channelId, 2, guess);
                                }
                                else
                                    _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200."));
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("bet", RunMode = RunMode.Async)]
        public async Task BetCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "bet"))
                    {
                        if (args.Length == 2)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong channelId = Context.Channel.Id;
                            ulong discordId = Context.User.Id;

                            string betStr = args[0];
                            string guessStr = args[1];

                            // if args are numbers
                            if (int.TryParse(guessStr, out int guess) && int.TryParse(betStr, out int bet))
                            {
                                // if guess number is valid
                                if (guess >= 1 && guess <= 200)
                                {
                                    StormPlayerDataEntity playerData = await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    if (bet <= 0)
                                    {
                                        _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please bet an amount above zero."));
                                    }
                                    else if (bet > playerData.Wallet)
                                    {
                                        _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have insufficient funds."));
                                    }
                                    else
                                    {
                                        await _service.TryToUpdateOngoingStorm(serverId, discordId, channelId, 2, guess, bet);
                                    }
                                }
                                else
                                    _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200."));
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("wallet", RunMode = RunMode.Async)]
        public async Task WalletCommand()
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallet"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormPlayerDataEntity playerData = await _service.GetStormPlayerDataEntity(serverId, discordId);

                        _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have {playerData.Wallet} points in your wallet."));
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("wallets", RunMode = RunMode.Async)]
        public async Task WalletsCommand()
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallets"))
                    {
                        List<string> output = new List<string>();
                        output.Add("```md\nSTORMS LEADERBOARD\n==================```");

                        List<StormPlayerDataEntity> playerData = await _service.GetAllStormPlayerDataEntities(Context.Guild.Id);

                        // sort list by amounts in players wallets
                        playerData = playerData.OrderByDescending(player => player.Wallet).ToList();

                        int playerCount = 1;
                        bool atleastOnePlayer = false;
                        foreach (StormPlayerDataEntity player in playerData)
                        {
                            if (player.Wallet > 0)
                            {
                                atleastOnePlayer = true;
                                output = ValidateOutputLimit(output, string.Format(@"**{0}.)** <@!{1}> has {2} points in their wallet.", playerCount, player.DiscordID, player.Wallet) + "\n");
                                playerCount++;
                            }
                        }

                        if (!atleastOnePlayer)
                            output = ValidateOutputLimit(output, "\n" + "No active players.");

                        if (output[0] != "")
                        {
                            foreach (string chunk in output)
                            {
                                _service.purgeCollection.Add(await ReplyAsync(chunk));
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }
        #endregion
    }
}
