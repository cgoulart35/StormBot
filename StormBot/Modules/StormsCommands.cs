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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "umbrella"))
                    {
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        await StormsService.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 1);
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "guess"))
                    {
                        if (args.Length == 1)
                        {
                            SocketGuild guild = Context.Guild;
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;

                            string guessStr = GetSingleArg(args);

                            // if arg is a number
                            if (int.TryParse(guessStr, out int guess))
                            {
                                // if number is valid
                                if (guess >= 1 && guess <= 200)
                                {
                                    StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                                    await StormsService.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess);
                                }
                                else
                                {
                                    IUserMessage message = await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200.");
                                    if (StormsService.PurgeCollection.ContainsKey(channelId))
                                        StormsService.PurgeCollection[channelId].Add(message);
                                }
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "bet"))
                    {
                        if (args.Length == 2)
                        {
                            SocketGuild guild = Context.Guild;
                            ulong serverId = Context.Guild.Id;
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
                                        IUserMessage message = await ReplyAsync($"<@!{discordId}>, please bet an amount above zero.");
                                        if (StormsService.PurgeCollection.ContainsKey(channelId))
                                            StormsService.PurgeCollection[channelId].Add(message);
                                    }
                                    else if (bet > StormsService.GetPlayerWallet(serverId, discordId))
                                    {
                                        IUserMessage message = await ReplyAsync($"<@!{discordId}>, you have insufficient funds.");
                                        if (StormsService.PurgeCollection.ContainsKey(channelId))
                                            StormsService.PurgeCollection[channelId].Add(message);
                                    }
                                    else
                                    {
                                        await StormsService.TryToUpdateOngoingStorm(guild, serverId, discordId, channelId, 2, guess, bet);
                                    }
                                }
                                else
                                {
                                    IUserMessage message = await ReplyAsync($"<@!{discordId}>, please provide a guess between 1 and 200.");
                                    if (StormsService.PurgeCollection.ContainsKey(channelId))
                                        StormsService.PurgeCollection[channelId].Add(message);
                                }
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "steal"))
                    {
                        SocketGuild guild = Context.Guild;
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        await StormsService.TryToSteal(guild, serverId, discordId, channelId);
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("insurance", RunMode = RunMode.Async)]
        public async Task BuyInsuranceCommand()
        {
            if (!Context.IsPrivate)
            {
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "insurance"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        if (StormsService.GetPlayerWallet(serverId, discordId) < StormsService.insuranceCost)
                        {
                            IUserMessage message = await ReplyAsync($"<@!{discordId}>, you have insufficient funds.");
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
                        }
                        else if (StormsService.GetPlayerInsurance(serverId, discordId))
                        {
                            IUserMessage message = await ReplyAsync($"<@!{discordId}>, you are already insured.");
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
                        }
                        else
                        {
                            StormsService.AddInsuranceForPlayer(serverId, discordId);

                            IUserMessage message = await ReplyAsync($"<@!{discordId}>, you purchased insurance for {StormsService.insuranceCost} points.");
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallet"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId = Context.User.Id;

                        StormsService.AddPlayerToDbTableIfNotExist(serverId, discordId);

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Green);
                        builder.WithTitle($"**{Context.Guild.GetUser(discordId).Username}'s Wallet**");
                        builder.WithThumbnailUrl(Context.User.GetAvatarUrl());
                        builder.AddField("Wallet", $"`{StormsService.GetPlayerWallet(serverId, discordId)}`", false);
                        builder.AddField("Insurance", StormsService.GetPlayerInsurance(serverId, discordId) ? "`INSURED`" : "`UNINSURED`", false);

                        IUserMessage message = await ReplyAsync("", false, builder.Build());
                        if (StormsService.PurgeCollection.ContainsKey(channelId))
                            StormsService.PurgeCollection[channelId].Add(message);
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "wallets"))
                    {
                        List<StormPlayerDataEntity> playerData = StormsService.GetAllStormPlayerDataEntities(Context.Guild.Id);

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Green);
                        builder.WithTitle("**Storms Wallet Leaderboard**");
                        builder.WithThumbnailUrl(Context.Guild.IconUrl);

                        int playerCount = 1;
                        bool atleastOnePlayer = false;
                        string playersStr = "";
                        string pointsStr = "";
                        string insuranceStr = "";
                        foreach (StormPlayerDataEntity player in playerData.OrderByDescending(player => player.Wallet))
                        {
                            if (player.Wallet > 0)
                            {
                                atleastOnePlayer = true;

                                playersStr += $"{playerCount}.) <@!{player.DiscordID}>\n";
                                pointsStr += $"`{player.Wallet}`\n";
                                insuranceStr += player.HasInsurance ? "`INSURED`\n" : "`UNINSURED`\n";

                                playerCount++;
                            }
                        }

                        if (!atleastOnePlayer)
                        {
                            IUserMessage message = await ReplyAsync("No active players.");
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
                        }
                        else
                        {
                            builder.AddField("Player", playersStr, true);
                            builder.AddField("Wallet", pointsStr, true);
                            builder.AddField("Insurance", insuranceStr, true);

                            IUserMessage message = await ReplyAsync("", false, builder.Build());
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
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
                ulong channelId = Context.Channel.Id;
                if (StormsService.PurgeCollection.ContainsKey(channelId))
                    StormsService.PurgeCollection[channelId].Add(Context.Message);

                if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "resets"))
                    {
                        List<StormPlayerDataEntity> playerData = StormsService.GetAllStormPlayerDataEntities(Context.Guild.Id);

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Green);
                        builder.WithTitle("**Storms Reset Leaderboard**");
                        builder.WithThumbnailUrl(Context.Guild.IconUrl);

                        int playerCount = 1;
                        bool atleastOnePlayer = false;
                        string playersStr = "";
                        string resetsStr = "";
                        foreach (StormPlayerDataEntity player in playerData.OrderByDescending(player => player.ResetCount))
                        {
                            if (player.ResetCount > 0)
                            {
                                atleastOnePlayer = true;

                                playersStr += $"{playerCount}.) <@!{player.DiscordID}>\n";
                                resetsStr += $"`{player.ResetCount}`\n";

                                playerCount++;
                            }
                        }

                        if (!atleastOnePlayer)
                        {
                            IUserMessage message = await ReplyAsync("No active players.");
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
                        }
                        else
                        {
                            builder.AddField("Player", playersStr, true);
                            builder.AddField("Resets", resetsStr, true);

                            IUserMessage message = await ReplyAsync("", false, builder.Build());
                            if (StormsService.PurgeCollection.ContainsKey(channelId))
                                StormsService.PurgeCollection[channelId].Add(message);
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
