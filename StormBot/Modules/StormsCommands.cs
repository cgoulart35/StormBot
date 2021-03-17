using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using StormBot.Services;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Modules
{
	public class StormsCommands : BaseCommand
    {
        private readonly StormsService _service;
        private readonly AnnouncementsService _announcementsService;

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
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "umbrella"))
                    {
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 1);
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
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "guess"))
                    {
                        if (args.Length == 1)
                        {
                            SocketGuild guild = Context.Guild;
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
                                    StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess);
                                }
                                else
                                    StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200."));
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
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "bet"))
                    {
                        if (args.Length == 2)
                        {
                            SocketGuild guild = Context.Guild;
                            ulong serverId = Context.Guild.Id;
                            ulong channelId = Context.Channel.Id;
                            ulong discordId = Context.User.Id;

                            string betStr = args[0];
                            string guessStr = args[1];

                            // if args are numbers
                            if (int.TryParse(guessStr, out int guess) && double.TryParse(betStr, out double bet))
                            {
                                // if guess number is valid
                                if (guess >= 1 && guess <= 200)
                                {
                                    StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    if (bet <= 0)
                                    {
                                        StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please bet an amount above zero."));
                                    }
                                    else if (bet > StormsService.GetPlayerWallet(serverId, discordId))
                                    {
                                        StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have insufficient funds."));
                                    }
                                    else
                                    {
                                        await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess, bet);
                                    }
                                }
                                else
                                    StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200."));
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("steal", RunMode = RunMode.Async)]
        public async Task StealCommand()
        {
            if (!Context.IsPrivate)
            {
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "steal"))
                    {
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        await _service.TryToSteal(guild, serverId, discordId, channelId);
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("buy insurance", RunMode = RunMode.Async)]
        public async Task BuyInsuranceCommand()
        {
            if (!Context.IsPrivate)
            {
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "buy insurance"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        if (StormsService.GetPlayerWallet(serverId, discordId) < StormsService.insuranceCost)
                        {
                            StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have insufficient funds."));
                        }
                        else if (StormsService.GetPlayerInsurance(serverId, discordId))
                        {
                            StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you are already insured."));
                        }
                        else
                        {
                            StormsService.AddInsuranceForPlayer(serverId, discordId);
                            StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you purchased insurance for {StormsService.insuranceCost} points."));
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
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallet"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        string insuranceStr = "";
                        if (StormsService.GetPlayerInsurance(serverId, discordId))
                            insuranceStr = "**INSURED**";
                        else
                            insuranceStr = "**UNINSURED**";

                        StormsService.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have {StormsService.GetPlayerWallet(serverId, discordId)} points in your wallet. " + insuranceStr));
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
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallets"))
                    {
                        List<string> output = new List<string>();
                        output.Add("```md\nSTORMS WALLET LEADERBOARD\n=========================```");

                        List<StormPlayerDataEntity> playerData = StormsService.GetAllStormPlayerDataEntities(Context.Guild.Id);

                        // sort list by amounts in players wallets
                        playerData = playerData.OrderByDescending(player => player.Wallet).ToList();

                        int playerCount = 1;
                        bool atleastOnePlayer = false;
                        foreach (StormPlayerDataEntity player in playerData)
                        {
                            if (player.Wallet > 0)
                            {
                                atleastOnePlayer = true;

                                string userStr = "";
                                if (player.DiscordID != Context.User.Id)
                                {
                                    SocketGuildUser user = Context.Guild.GetUser(player.DiscordID);
                                    userStr = user.Username;
                                }
                                else
                                {
                                    userStr = $"<@!{player.DiscordID}>";
                                }

                                string insuranceStr = "";
                                if (player.HasInsurance)
                                    insuranceStr = "**INSURED**";
                                else
                                    insuranceStr = "**UNINSURED**";

                                output = ValidateOutputLimit(output, string.Format(@"**{0}.)** {1} has {2} points in their wallet. {3}", playerCount, userStr, player.Wallet, insuranceStr) + "\n");
                                playerCount++;
                            }
                        }

                        if (!atleastOnePlayer)
                            output = ValidateOutputLimit(output, "\n" + "No active players.");

                        if (output[0] != "")
                        {
                            foreach (string chunk in output)
                            {
                                StormsService.purgeCollection.Add(await ReplyAsync(chunk));
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("resets", RunMode = RunMode.Async)]
        public async Task ResetsCommand()
        {
            if (!Context.IsPrivate)
            {
                StormsService.purgeCollection.Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "resets"))
                    {
                        List<string> output = new List<string>();
                        output.Add("```md\nSTORMS RESET LEADERBOARD\n========================```");

                        List<StormPlayerDataEntity> playerData = StormsService.GetAllStormPlayerDataEntities(Context.Guild.Id);

                        // sort list by amounts in players wallets
                        playerData = playerData.OrderByDescending(player => player.ResetCount).ToList();

                        int playerCount = 1;
                        bool atleastOnePlayer = false;
                        foreach (StormPlayerDataEntity player in playerData)
                        {
                            if (player.ResetCount > 0)
                            {
                                atleastOnePlayer = true;

                                string userStr = "";
                                if (player.DiscordID != Context.User.Id)
                                {
                                    SocketGuildUser user = Context.Guild.GetUser(player.DiscordID);
                                    userStr = user.Username;
                                }
                                else
                                {
                                    userStr = $"<@!{player.DiscordID}>";
                                }

                                output = ValidateOutputLimit(output, string.Format(@"**{0}.)** {1} has {2} resets.", playerCount, userStr, player.ResetCount) + "\n");
                                playerCount++;
                            }
                        }

                        if (!atleastOnePlayer)
                            output = ValidateOutputLimit(output, "\n" + "No active players.");

                        if (output[0] != "")
                        {
                            foreach (string chunk in output)
                            {
                                StormsService.purgeCollection.Add(await ReplyAsync(chunk));
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
