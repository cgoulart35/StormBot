using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 1);

                        await _service.CheckForReset(guild, serverId, discordId, channelId);
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
                                    await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess);

                                    await _service.CheckForReset(guild, serverId, discordId, channelId);
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
                                        await _service.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess, bet);

                                        await _service.CheckForReset(guild, serverId, discordId, channelId);
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

        [Command("buy insurance", RunMode = RunMode.Async)]
        public async Task BuyInsuranceCommand()
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "buy insurance"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormPlayerDataEntity playerData = await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        if (playerData.Wallet < _service.insuranceCost)
                        {
                            _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have insufficient funds."));
                        }
                        else if (playerData.HasInsurance)
                        {
                            _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you are already insured."));
                        }
                        else
                        {
                            playerData.Wallet -= _service.insuranceCost;
                            playerData.HasInsurance = true;
                            await _service._db.SaveChangesAsync();

                            _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you purchased insurance for {_service.insuranceCost} points."));
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("cause disaster", RunMode = RunMode.Async)]
        public async Task CauseDisasterCommand()
        {
            if (!Context.IsPrivate)
            {
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "cause disaster"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        StormPlayerDataEntity playerData = await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        if (playerData.Wallet < _service.disasterCost)
                        {
                            _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have insufficient funds."));
                        }
                        else
                        {
                            if (_service.IsOngoingStorm(channelId))
                            {
                                playerData.Wallet -= _service.disasterCost;

                                // reset random player's wallet if they are uninsured
                                List<StormPlayerDataEntity> allPlayerData = await _service.GetAllStormPlayerDataEntities(Context.Guild.Id);
                                Random random = new Random();
                                int randomIndex = random.Next(0, allPlayerData.Count);

                                string theyYouStr = "";
                                string theirYour = "";
                                string onPersonAffected = "";
                                if (allPlayerData[randomIndex].DiscordID == discordId)
                                {
                                    theyYouStr = " You";
                                    theirYour = " your";
                                    onPersonAffected = " on yourself";
                                }
                                else
                                {
                                    theyYouStr = " They";
                                    theirYour = " their";
                                    onPersonAffected = $" for <@!{allPlayerData[randomIndex].DiscordID}>";
                                }

                                string insuredOrNotStr = "";
                                if (!allPlayerData[randomIndex].HasInsurance)
                                {
                                    insuredOrNotStr = theyYouStr + " were not insured and" + theirYour + " wallet has been reset!";
                                    allPlayerData[randomIndex].Wallet = _service.resetBalance;
                                }
                                else
                                {
                                    insuredOrNotStr = " However," + theyYouStr + " were insured and not affected.";
                                }

                                await _service._db.SaveChangesAsync();

                                _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you caused a disaster" + onPersonAffected + $" for {_service.disasterCost} points!" + insuredOrNotStr));
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
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "steal"))
                    {
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong discordId = Context.User.Id;

                        if (_service.IsOngoingStorm(channelId))
                        {
                            StormPlayerDataEntity playerData = await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);
                            List<StormPlayerDataEntity> allPlayerData = await _service.GetAllStormPlayerDataEntities(serverId);
                            StormPlayerDataEntity topPlayer = allPlayerData.OrderByDescending(player => player.Wallet).First();

                            // do not let users steal from themselves
                            if (topPlayer.DiscordID != discordId)
                            {
                                // set top player's wallet and criminal's wallet
                                double oldWallet = topPlayer.Wallet;
                                double newWallet = oldWallet - _service.stealAmount;
                                double diff;
                                if (newWallet < 0)
                                {
                                    topPlayer.Wallet = 0;
                                    diff = oldWallet;
                                }
                                else
                                {
                                    topPlayer.Wallet = newWallet;
                                    diff = _service.stealAmount;
                                }

                                //playerData.Wallet += diff;

                                await _service._db.SaveChangesAsync();

                                _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you stole {diff} points from <@!{topPlayer.DiscordID}>!"));

                                await _service.CheckForReset(guild, serverId, discordId, channelId);
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

                        StormPlayerDataEntity playerData = await _service.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        string insuranceStr = "";
                        if (playerData.HasInsurance)
                            insuranceStr = "**INSURED**";
                        else
                            insuranceStr = "**UNINSURED**";

                        _service.purgeCollection.Add(await ReplyAsync($"<@!{discordId}>, you have {playerData.Wallet} points in your wallet. " + insuranceStr));
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
                        output.Add("```md\nSTORMS WALLET LEADERBOARD\n=========================```");

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
                                _service.purgeCollection.Add(await ReplyAsync(chunk));
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
                _service.purgeCollection.Add(Context.Message);

                if (await _service.GetServerAllowServerPermissionStorms(Context) && await _service.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "resets"))
                    {
                        List<string> output = new List<string>();
                        output.Add("```md\nSTORMS RESET LEADERBOARD\n========================```");

                        List<StormPlayerDataEntity> playerData = await _service.GetAllStormPlayerDataEntities(Context.Guild.Id);

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
