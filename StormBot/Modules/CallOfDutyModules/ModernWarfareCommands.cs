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
	public class ModernWarfareCommands : BaseCallOfDutyCommands
    {
        private readonly CallOfDutyService _service;
        private readonly AnnouncementsService _announcementsService;

        static bool handlersSet = false;

        public ModernWarfareCommands(IServiceProvider services)
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
            if (DisableIfServiceNotRunning(_service.ModernWarfareComponent))
            {
                List<ulong> serverIds = CallOfDutyService.GetAllValidatedServerIds("mw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = CallOfDutyService.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        // pass true to keep track of lifetime total kills every week
                        List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(true, serverId, "mw", "mp");

                        SocketGuild guild = CallOfDutyService._client.GetGuild(serverId);

                        EmbedBuilder builder = await GetLast7DaysKills(newData, guild);

                        if (builder != null)
                            await channel.SendMessageAsync("", false, builder.Build());
                        else
                            await channel.SendMessageAsync("No data returned.");
                    }
                }
            }
        }

        public async Task DailyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service.ModernWarfareComponent))
            {
                List<ulong> serverIds = CallOfDutyService.GetAllValidatedServerIds("mw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = CallOfDutyService.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, serverId, "mw", "mp");

                        SocketGuild guild = CallOfDutyService._client.GetGuild(serverId);

                        EmbedBuilder builder = GetWeeklyKills(newData, guild);

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
                builder.WithTitle("**Modern Warfare Kills In Last 7 Days**");

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

                    ulong roleID = CallOfDutyService.GetServerModernWarfareKillsRoleID(guild.Id);
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

                    string messageStr = string.Format(@"Congratulations{0} you have the most Modern Warfare kills out of all Modern Warfare participants in the last 7 days!{1}", winners, roleStr);

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

		public EmbedBuilder GetWeeklyKills(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null)
		{
			if (guild == null)
				guild = Context.Guild;

			List<CallOfDutyPlayerModel> storedData = CallOfDutyService.GetServersPlayerDataAsPlayerModelList(guild.Id, "mw", "mp");
			List<CallOfDutyPlayerModel> outputPlayers = new List<CallOfDutyPlayerModel>();

            if (newData != null && storedData != null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(Color.Purple);
                builder.WithTitle("**Modern Warfare & Warzone Weekly Kills**");

                // set weekly kill counts
                foreach (CallOfDutyPlayerModel player in newData)
                {
                    CallOfDutyPlayerModel outputPlayer = player;

                    double kills = 0;

                    // if user has played at all
                    if (player.Data.Lifetime.All.Properties != null)
                    {
                        // if player kill count saved
                        if (storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID) != null)
                        {
                            // if player kill count missed last data fetch, set kills = -1 (postpone daily posting until after next data fetch)
                            if (CallOfDutyService.MissedLastDataFetch(guild.Id, player.DiscordID, "mw", "mp"))
                                kills = -1;
                            // if player kill count has last data fetch, set kills this week
                            else
                                kills = player.Data.Lifetime.All.Properties.Kills - storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID).Data.Lifetime.All.Properties.Kills;
                        }
                        // if player kill count not saved, set kills = -1 (postpone daily posting until after next data fetch)
                        else
                            kills = -1;
                    }

                    if (kills != 0)
                    {
                        outputPlayer.Data.Lifetime.All.Properties.Kills = kills;
                        outputPlayers.Add(outputPlayer);
                    }
                }

                // sort weekly kill counts & print weekly kills

                int playerCount = 1;
                bool atleastOnePlayer = false;
                string playersStr = "";
                string killsStr = "";
                string nextWeekMessages = "";
                outputPlayers = outputPlayers.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();
                foreach (CallOfDutyPlayerModel player in outputPlayers)
                {
                    if (player.Data.Lifetime.All.Properties.Kills == -1)
                        nextWeekMessages += string.Format(@"<@!{0}> will be included in updates starting next week once the user's data is updated.", player.DiscordID) + "\n";
                    else
                    {
                        atleastOnePlayer = true;

                        playersStr += $"`{playerCount}.)` <@!{player.DiscordID}>\n";
                        killsStr += $"`{player.Data.Lifetime.All.Properties.Kills}`\n";

                        playerCount++;
                    }
                }

                // never update roles here; updated only once every week
                // weekly kills at end of competition should be equal to last 7 days values from API

                if (atleastOnePlayer)
                {
                    double topScore = outputPlayers[0].Data.Lifetime.All.Properties.Kills;
                    List<ulong> topPlayersDiscordIDs = outputPlayers.Where(player => player.Data.Lifetime.All.Properties?.Kills == topScore).Select(player => player.DiscordID).ToList();

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@"<@!{0}>, ", DiscordID);
                    }

                    string messageStr = string.Format(@"{0}you are currently in the lead with the most Modern Warfare + Warzone kills this week!", winners);

                    builder.WithDescription(messageStr + "\n\n" + nextWeekMessages);
                    builder.AddField("Player", playersStr, true);
                    builder.AddField("Kills", killsStr, true);
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
		[Command("mw wz weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw wz weekly kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "mp");

                            EmbedBuilder builder = GetWeeklyKills(newData);

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
        [Command("mw wz lifetime kills", RunMode = RunMode.Async)]
        public async Task LifetimeKillsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw wz lifetime kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "mp");

                            if (newData != null)
                            {
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Purple);
                                builder.WithTitle("**Modern Warfare & Warzone Lifetime Kills**");
                                builder.WithThumbnailUrl(Context.Guild.IconUrl);

                                int playerCount = 1;
                                bool atleastOnePlayer = false;
                                string playersStr = "";
                                string killsStr = "";
                                newData = newData.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();
                                foreach (CallOfDutyPlayerModel player in newData)
                                {
                                    double kills = 0;

                                    // if user has not played
                                    if (player.Data.Lifetime.All.Properties == null || player.Data.Lifetime.All.Properties.Kills == 0)
                                        continue;
                                    // if user has played
                                    else
                                    {
                                        atleastOnePlayer = true;
                                        kills = player.Data.Lifetime.All.Properties.Kills;
                                    }

                                    playersStr += $"`{playerCount}.)` <@!{player.DiscordID}>\n";
                                    killsStr += $"`{kills}`\n";

                                    playerCount++;
                                }

                                if (atleastOnePlayer)
                                {
                                    double topScore = newData[0].Data.Lifetime.All.Properties.Kills;
                                    List<ulong> topPlayersDiscordIDs = newData.Where(player => player.Data.Lifetime.All.Properties?.Kills == topScore).Select(player => player.DiscordID).ToList();

                                    string winners = "Congratulations ";
                                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                                    {
                                        winners += string.Format(@"<@!{0}>, ", DiscordID);
                                    }

                                    string messageStr = $"{winners}you have the most Modern Warfare + Warzone kills in your lifetime out of all Modern Warfare participants!";

                                    builder.WithDescription(messageStr);
                                    builder.AddField("Player", playersStr, true);
                                    builder.AddField("Kills", killsStr, true);

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
        [Command("mw participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw participants"))
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
                            string modeAbbrev = "mp";

                            await ListPartcipants(serverID, gameAbbrev, modeAbbrev);
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("mw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw add participant"))
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
                                string modeAbbrev = "mp";

                                if (await AddAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been added to the Modern Warfare participant list.", discordID));
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
        [Command("mw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw rm participant"))
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
                                string modeAbbrev = "mp";

                                if (await RemoveAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been removed from the Modern Warfare participant list.", discordID));
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

        [Command("mw participate", RunMode = RunMode.Async)]
        public async Task ParticipateCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "mw participate"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "mp";

                            if (await AddAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been added to the Modern Warfare participant list.", discordID));
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

        [Command("mw leave", RunMode = RunMode.Async)]
        public async Task LeaveCommand()
        {
            if (!Context.IsPrivate)
            {
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "mw leave"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "mp";

                            if (await RemoveAParticipant(serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been removed from the Modern Warfare participant list.", discordID));
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
