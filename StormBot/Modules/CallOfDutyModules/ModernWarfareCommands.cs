using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Services;
using StormBot.Models.CallOfDutyModels;
using StormBot.Database;

namespace StormBot.Modules.CallOfDutyModules
{
	public class ModernWarfareCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService _service;
        private AnnouncementsService _announcementsService;

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
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("mw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        // pass true to keep track of lifetime total kills every week
                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(true, serverId, "mw", "mp");

                        SocketGuild guild = _service._client.GetGuild(serverId);

                        List<string> output = await GetLast7DaysKills(newData, guild);

                        if (output[0] != "")
                        {
                            foreach (string chunk in output)
                            {
                                await channel.SendMessageAsync(chunk);
                            }
                        }
                    }
                }
            }
        }

        public async Task DailyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service.ModernWarfareComponent))
            {
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("mw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, serverId, "mw", "mp");

                        SocketGuild guild = _service._client.GetGuild(serverId);

                        List<string> output = await GetWeeklyKills(newData, guild);

                        if (output[0] != "")
                        {
                            foreach (string chunk in output)
                            {
                                await channel.SendMessageAsync(chunk);
                            }
                        }
                    }
                }
            }
        }

        public async Task<List<string>> GetLast7DaysKills(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            List<string> output = new List<string>();

            if (newData != null)
            {
                output.Add("```md\nMODERN WARFARE KILLS IN LAST 7 DAYS\n===================================```");
                newData = newData.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();

                int playerCount = 1;
                bool atleastOnePlayer = false;
                foreach (CallOfDutyPlayerModel player in newData)
                {
                    double kills = 0;

                    // if user has not played this week
                    if (player.Data.Weekly.All.Properties == null)
                        continue;
                    // if user has played this week
                    else
                    {
                        atleastOnePlayer = true;
                        kills = player.Data.Weekly.All.Properties.Kills;
                    }

                    output = ValidateOutputLimit(output, string.Format(@"**{0}.)** <@!{1}> has {2} kills in the last 7 days.", playerCount, player.DiscordID, kills) + "\n");
                    
                    playerCount++;
                }

                if (atleastOnePlayer)
                {
                    double topScore = newData[0].Data.Weekly.All.Properties.Kills;
                    List<ulong> topPlayersDiscordIDs = newData.Where(player => player.Data.Weekly.All.Properties?.Kills == topScore).Select(player => player.DiscordID).ToList();

                    ulong roleID = await _service.GetServerModernWarfareKillsRoleID(guild.Id);
                    string roleStr = "";
                    if (roleID != 0)
                    {
                        await UnassignRoleFromAllMembers(roleID, guild);
                        await GiveUsersRole(roleID, topPlayersDiscordIDs, guild);
                        roleStr = $" You have been assigned the role <@&{roleID}>!";
                    }

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@" <@!{0}>,", DiscordID);
                    }

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations{0} you have the most Modern Warfare kills out of all Modern Warfare participants in the last 7 days!{1}", winners, roleStr));
                }
                else
                    output = ValidateOutputLimit(output, "\n" + "No active players this week.");

                return output;
            }
            else
            {
                output.Add("No data returned.");
                return output;
            }
        }

        public async Task <List<string>> GetWeeklyKills(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            List<CallOfDutyPlayerModel> storedData = await _service.GetServersPlayerDataAsPlayerModelList(guild.Id, "mw", "mp");
            List<CallOfDutyPlayerModel> outputPlayers = new List<CallOfDutyPlayerModel>();

            List<string> output = new List<string>();

            if (newData != null && storedData != null)
            {
                output.Add("```md\nMODERN WARFARE & WARZONE WEEKLY KILLS\n=====================================```");

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
                            if (await _service.MissedLastDataFetch(guild.Id, player.DiscordID, "mw", "mp"))
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

                // sort weekly kill counts
                outputPlayers = outputPlayers.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                // print weekly kills
                int playerCount = 1;
                bool atleastOnePlayer = false;
                string nextWeekMessages = "";
                foreach (CallOfDutyPlayerModel player in outputPlayers)
                {
                    if (player.Data.Lifetime.All.Properties.Kills == -1)
                        nextWeekMessages += string.Format(@"<@!{0}> will be included in updates starting next week once the user's data is updated.", player.DiscordID) + "\n";
                    else
                    {
                        atleastOnePlayer = true;
                        output = ValidateOutputLimit(output, string.Format(@"**{0}.)** <@!{1}> has {2} kills so far this week.", playerCount, player.DiscordID, player.Data.Lifetime.All.Properties.Kills) + "\n");
                        playerCount++;
                    }
                }

                if (nextWeekMessages != "")
                    output = ValidateOutputLimit(output, "\n" + nextWeekMessages);

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

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"{0}you are currently in the lead with the most Modern Warfare + Warzone kills this week!", winners));
                }
                else
                    output = ValidateOutputLimit(output, "\n" + "No active players this week.");

                return output;
            }
            else
            {
                output.Add("No data returned.");
                return output;
            }
        }

        #region COMMAND FUNCTIONS
        // admin role command
        [Command("mw wz weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw wz weekly kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "mp");

                            List<string> output = await GetWeeklyKills(newData);
                            if (output[0] != "")
                            {
                                foreach (string chunk in output)
                                {
                                    await ReplyAsync(chunk);
                                }
                            }
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw wz lifetime kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "mp");

                            if (newData != null)
                            {
                                List<string> output = new List<string>();
                                output.Add("```md\nMODERN WARFARE & WARZONE LIFETIME KILLS\n=======================================```");

                                newData = newData.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                                int playerCount = 1;
                                bool atleastOnePlayer = false;
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

                                    output = ValidateOutputLimit(output, string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, kills) + "\n");

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

                                    output = ValidateOutputLimit(output, "\n" + $"{winners}you have the most Modern Warfare + Warzone kills in your lifetime out of all Modern Warfare participants!");

                                    if (output[0] != "")
                                    {
                                        foreach (string chunk in output)
                                        {
                                            await ReplyAsync(chunk);
                                        }
                                    }
                                }
                                else
                                {
                                    output = ValidateOutputLimit(output, "\n" + "No active players.");

                                    if (output[0] != "")
                                    {
                                        foreach (string chunk in output)
                                        {
                                            await ReplyAsync(chunk);
                                        }
                                    }
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw participants"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            ulong serverID = Context.Guild.Id;
                            string gameAbbrev = "mw";
                            string modeAbbrev = "mp";

                            await ListPartcipants(_service, serverID, gameAbbrev, modeAbbrev);
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw add participant"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
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

                                if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "mw rm participant"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
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

                                if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
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

                            if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await _service.GetServerAllowServerPermissionModernWarfareTracking(Context) && await _service.GetServerToggleModernWarfareTracking(Context))
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

                            if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
