﻿using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Services;
using StormBot.Models.CallOfDutyModels;

namespace StormBot.Modules.CallOfDutyModules
{
    public class WarzoneCommands : BaseCallOfDutyCommands
    {
        private readonly CallOfDutyService _service;
        private readonly AnnouncementsService _announcementsService;

        static bool handlersSet = false;

        public WarzoneCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService>();
            _announcementsService = services.GetRequiredService<AnnouncementsService>();

            if (!handlersSet)
            {
                _announcementsService.WeeklyCallOfDutyAnnouncement += WeeklyCompetitionUpdates;
                _announcementsService.DailyCallOfDutyAnnouncement += DailyCompetitionUpdates;

                handlersSet = true;
            }
        }

        public async Task WeeklyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service.WarzoneComponent))
            {
                List<ulong> serverIds = CallOfDutyService.GetAllValidatedServerIds("mw", "wz");
                foreach (ulong serverId in serverIds)
                {
                    var channel = CallOfDutyService.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        // since GetWeeklyWins does not get the last 7 day count from API, but instead calculates the difference like daily updates, database player data needs to be retrieved in memory to be used for calculation before overwritten with GetNewPlayerData function
                        List<CallOfDutyPlayerModel> oldData = CallOfDutyService.GetServersPlayerDataAsPlayerModelList(serverId, "mw", "wz");

                        // pass true to keep track of lifetime total kills every week
                        List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(true, serverId, "mw", "wz");

                        SocketGuild guild = CallOfDutyService._client.GetGuild(serverId);

                        EmbedBuilder builder1 = await GetLast7DaysKills(newData, guild);
                        EmbedBuilder builder2 = await GetWeeklyWins(newData, guild, true, oldData);

                        if (builder1 != null)
                            await channel.SendMessageAsync("", false, builder1.Build());
                        else
                            await channel.SendMessageAsync("No data returned.");

                        if (builder2 != null)
                            await channel.SendMessageAsync("", false, builder2.Build());
                        else
                            await channel.SendMessageAsync("No data returned.");
                    }
                }
            }
        }

        public async Task DailyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service.WarzoneComponent))
            {
                List<ulong> serverIds = CallOfDutyService.GetAllValidatedServerIds("mw", "wz");
                foreach (ulong serverId in serverIds)
                {
                    var channel = CallOfDutyService.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, serverId, "mw", "wz");

                        SocketGuild guild = CallOfDutyService._client.GetGuild(serverId);

                        EmbedBuilder builder = await GetWeeklyWins(newData, guild);

                        if (builder != null)
                            await channel.SendMessageAsync("", false, builder.Build());
                        else
                            await channel.SendMessageAsync("No data returned.");
                    }
                }
            }
        }

        public async Task<EmbedBuilder> GetLast7DaysKills(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            if (newData != null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(Color.Purple);
                builder.WithTitle("**Warzone Kills In Last 7 Days**");

                int playerCount = 1;
                bool atleastOnePlayer = false;
                string playersStr = "";
                string killsStr = "";
                newData = newData.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();
                foreach (CallOfDutyPlayerModel player in newData)
                {
                    double kills = 0;

                    // if user has not played this week
                    if (player.Data.Weekly.All.Properties == null || player.Data.Weekly.All.Properties.Kills == 0)
                        continue;
                    // if user has played this week
                    else
                    {
                        atleastOnePlayer = true;
                        kills = player.Data.Weekly.All.Properties.Kills;
                    }

                    playersStr += $"`{playerCount}.)` <@!{player.DiscordID}>\n";
                    killsStr += $"`{kills}`\n";

                    playerCount++;
                }

                if (atleastOnePlayer)
                {
                    double topScore = newData[0].Data.Weekly.All.Properties.Kills;
                    List<ulong> topPlayersDiscordIDs = newData.Where(player => player.Data.Weekly.All.Properties?.Kills == topScore).Select(player => player.DiscordID).ToList();

                    ulong roleID = CallOfDutyService.GetServerWarzoneKillsRoleID(guild.Id);
                    string roleStr = "";
                    if (roleID != 0)
                    {
                        await BaseService.UnassignRoleFromAllMembers(roleID, guild);
                        await BaseService.GiveUsersRole(roleID, topPlayersDiscordIDs, guild);
                        roleStr = $" You have been assigned the role <@&{roleID}>!";
                    }

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@" <@!{0}>,", DiscordID);
                    }

                    string messageStr = string.Format(@"Congratulations{0} you have the most Warzone kills out of all Warzone participants in the last 7 days!{1}", winners, roleStr);

                    builder.WithDescription(messageStr);
                    builder.AddField("Player", playersStr, true);
                    builder.AddField("Kills", killsStr, true);
                }
                else
                    builder.WithDescription("No active players this week.");

                return builder;
            }
            else
                return null;
        }

        public async Task<EmbedBuilder> GetWeeklyWins(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null, bool updateRoles = false, List<CallOfDutyPlayerModel> storedData = null)
        {
            if (guild == null)
                guild = Context.Guild;

            // only get current data in database if old data is not provided for comparison
            if (!updateRoles || storedData == null)
                storedData = CallOfDutyService.GetServersPlayerDataAsPlayerModelList(guild.Id, "mw", "wz");
            
            List<CallOfDutyPlayerModel> outputPlayers = new List<CallOfDutyPlayerModel>();

            if (newData != null && storedData != null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(Color.Purple);
                builder.WithTitle("**Warzone Weekly Wins**");

                // set weekly win counts
                foreach (CallOfDutyPlayerModel player in newData)
                {
                    CallOfDutyPlayerModel outputPlayer = player;

                    double wins = 0;

                    // if user has played at all
                    if (player.Data.Lifetime.Mode.BattleRoyal.Properties != null)
                    {
                        // if player win count saved
                        if (storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID) != null)
                        {
                            // if player win count missed last data fetch, set wins = -1 (postpone daily posting until after next data fetch)
                            if (CallOfDutyService.MissedLastDataFetch(guild.Id, player.DiscordID, "mw", "wz"))
                                wins = -1;
                            // if player win count has last data fetch, set wins this week
                            else
                                wins = player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins - storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID).Data.Lifetime.Mode.BattleRoyal.Properties.Wins;
                        }
                        // if player win count not saved, set wins = -1 (postpone daily posting until after next data fetch)
                        else
                            wins = -1;
                    }

                    if (wins != 0)
                    {
                        outputPlayer.Data.Lifetime.Mode.BattleRoyal.Properties.Wins = wins;
                        outputPlayers.Add(outputPlayer);
                    }
                }

                // sort weekly win count & print weekly wins

                int playerCount = 1;
                bool atleastOnePlayer = false;
                string playersStr = "";
                string winsStr = "";
                string nextWeekMessages = "";
                outputPlayers = outputPlayers.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();
                foreach (CallOfDutyPlayerModel player in outputPlayers)
                {
                    if (player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins == -1)
                        nextWeekMessages += string.Format(@"<@!{0}> will be included in updates starting next week once the user's data is updated.", player.DiscordID) + "\n";
                    else
                    {
                        atleastOnePlayer = true;

                        playersStr += $"`{playerCount}.)` <@!{player.DiscordID}>\n";
                        winsStr += $"`{player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins}`\n";

                        playerCount++;
                    }
                }

                if (atleastOnePlayer)
                {
                    double topScore = outputPlayers[0].Data.Lifetime.Mode.BattleRoyal.Properties.Wins;
                    List<ulong> topPlayersDiscordIDs = outputPlayers.Where(player => player.Data.Lifetime.Mode.BattleRoyal.Properties?.Wins == topScore).Select(player => player.DiscordID).ToList();

                    // update the roles only every week
                    ulong roleID = CallOfDutyService.GetServerWarzoneWinsRoleID(guild.Id);
                    string roleStr = "";
                    if (updateRoles && roleID != 0)
                    {
                        await BaseService.UnassignRoleFromAllMembers(roleID, guild);
                        await BaseService.GiveUsersRole(roleID, topPlayersDiscordIDs, guild);
                        roleStr = $" You have been assigned the role <@&{roleID}>!";
                    }

                    string winners = updateRoles ? "Congratulations " : "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@"<@!{0}>, ", DiscordID);
                    }

                    string messageStr = string.Format(@"{0}you have the most Warzone wins out of all Warzone participants!{1}", winners, roleStr);

                    builder.WithDescription(messageStr + "\n\n" + nextWeekMessages);
                    builder.AddField("Player", playersStr, true);
                    builder.AddField("Wins", winsStr, true);
                }
                else
                    builder.WithDescription("No active players this week." + "\n\n" + nextWeekMessages);

                return builder;
            }
            else
                return null;
        }

        #region COMMAND FUNCTIONS
        // admin role command
        [Command("wz weekly wins", RunMode = RunMode.Async)]
        public async Task WeeklyWinsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.WarzoneComponent, "wz wins"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "wz");

                            EmbedBuilder builder = await GetWeeklyWins(newData);

                            if (builder != null)
                                await ReplyAsync("", false, builder.Build());
                            else
                                await ReplyAsync("No data returned.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("wz lifetime wins", RunMode = RunMode.Async)]
        public async Task LifetimeWinsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.WarzoneComponent, "wz wins"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "wz");

                            if (newData != null)
                            {
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Purple);
                                builder.WithTitle("**Warzone Lifetime Wins**");
                                builder.WithThumbnailUrl(Context.Guild.IconUrl);

                                int playerCount = 1;
                                bool atleastOnePlayer = false;
                                string playersStr = "";
                                string winsStr = "";
                                newData = newData.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();
                                foreach (CallOfDutyPlayerModel player in newData)
                                {
                                    double wins = 0;

                                    // if user has not played
                                    if (player.Data.Lifetime.Mode == null || player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins == 0)
                                        continue;
                                    // if user has played
                                    else
                                    {
                                        atleastOnePlayer = true;
                                        wins = player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins;
                                    }

                                    playersStr += $"`{playerCount}.)` <@!{player.DiscordID}>\n";
                                    winsStr += $"`{wins}`\n";

                                    playerCount++;
                                }

                                if (atleastOnePlayer)
                                {
                                    double topScore = newData[0].Data.Lifetime.Mode.BattleRoyal.Properties.Wins;
                                    List<ulong> topPlayersDiscordIDs = newData.Where(player => player.Data.Lifetime.Mode.BattleRoyal.Properties?.Wins == topScore).Select(player => player.DiscordID).ToList();

                                    string winners = "Congratulations ";
                                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                                    {
                                        winners += string.Format(@"<@!{0}>, ", DiscordID);
                                    }

                                    string messageStr = $"{winners}you have the most Warzone wins out of all Warzone participants!";

                                    builder.WithDescription(messageStr);
                                    builder.AddField("Player", playersStr, true);
                                    builder.AddField("Wins", winsStr, true);

                                    await ReplyAsync("", false, builder.Build());
                                }
                                else
                                {
                                    builder.WithDescription("No active players.");

                                    await ReplyAsync("", false, builder.Build());
                                }
                            }
                            else
                            {
                                await ReplyAsync("No data returned.");
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("wz participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz participants"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            ulong serverID = Context.Guild.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "wz";

                            await ListPartcipants(serverID, gameAbbrev, modeAbbrev);
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("wz add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz add participant"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            try
                            {
                                string input = GetSingleArg(args);
                                ulong serverID = Context.Guild.Id;
                                ulong discordID = GetDiscordID(input);
                                string gameAbbrev = "mw";
                                string modeAbbrev = "wz";

                                if (await AddAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been added to the Warzone participant list.", discordID));
                                else
                                    await ReplyAsync(string.Format("<@!{0}> was not added.", discordID));
                            }
                            catch
                            {
                                await ReplyAsync("Please provide a valid Discord user.");
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("wz rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz rm participant"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            try
                            {
                                string input = GetSingleArg(args);
                                ulong serverID = Context.Guild.Id;
                                ulong discordID = GetDiscordID(input);
                                string gameAbbrev = "mw";
                                string modeAbbrev = "wz";

                                if (await RemoveAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been removed from the Warzone participant list.", discordID));
                            }
                            catch
                            {
                                await ReplyAsync("Please provide a valid Discord user.");
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("wz participate", RunMode = RunMode.Async)]
        public async Task ParticipateCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "wz participate"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "wz";

                            if (await AddAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been added to the Warzone participant list.", discordID));
                            else
                                await ReplyAsync(string.Format("<@!{0}> was not added.", discordID));
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord user.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("wz leave", RunMode = RunMode.Async)]
        public async Task LeaveCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "wz leave"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "wz";

                            if (await RemoveAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been removed from the Warzone participant list.", discordID));
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord user.");
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
