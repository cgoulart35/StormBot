using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using cg_bot.Services;
using cg_bot.Models.CallOfDutyModels;
using cg_bot.Database;

namespace cg_bot.Modules.CallOfDutyModules
{
    public class BlackOpsColdWarCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService _service;
        private AnnouncementsService _announcementsService;
        private CgBotContext _db;

        static bool handlersSet = false;

        public BlackOpsColdWarCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService>();
            _db = services.GetRequiredService<CgBotContext>();
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
            if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent))
            {
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("cw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        await channel.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S WINNERS!!!! CONGRATULATIONS!!!\n```");

                        // pass true to keep track of lifetime total kills every week
                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(true, serverId, "cw", "mp");

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
            if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent))
            {
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("cw", "mp");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        await channel.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S CURRENT RANKINGS!\n```");

                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, serverId, "cw", "mp");

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
                output.Add("```md\nBLACK OPS COLD WAR KILLS IN LAST 7 DAYS\n=======================================```");
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

                    ulong roleID = await _service.GetServerBlackOpsColdWarKillsRoleID(guild.Id);
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

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations{0} you have the most kills out of all Black Ops Cold War participants in the last 7 days!{1}", winners, roleStr));
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

        public async Task<List<string>> GetWeeklyKills(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            List<CallOfDutyPlayerModel> storedData = await _service.GetServersPlayerDataAsPlayerModelList(guild.Id, "cw", "mp");
            List<CallOfDutyPlayerModel> outputPlayers = new List<CallOfDutyPlayerModel>();

            List<string> output = new List<string>();

            if (newData != null && storedData != null)
            {
                output.Add("```md\nBLACK OPS COLD WAR WEEKLY KILLS\n===============================```");

                // set weekly kill counts
                foreach (CallOfDutyPlayerModel player in newData)
                {
                    CallOfDutyPlayerModel outputPlayer = player;

                    double kills = 0;

                    // if user has played at all
                    if (player.Data.Lifetime.All.Properties != null)
                    {
                        // if player kill count saved last week, set kills this week
                        if (storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID) != null)
                            kills = player.Data.Lifetime.All.Properties.Kills - storedData.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID).Data.Lifetime.All.Properties.Kills;
                        // if player kill count not saved last week, set kills = -1
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
                        nextWeekMessages += string.Format(@"<@!{0}> will be included in daily updates starting next week.", player.DiscordID) + "\n";
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

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"{0}you are currently in the lead with the most Black Ops Cold War kills this week!", winners));
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
        [Command("bocw weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw weekly kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, Context.Guild.Id, "cw", "mp");

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
        [Command("bocw lifetime kills", RunMode = RunMode.Async)]
        public async Task LifetimeKillsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw lifetime kills"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, Context.Guild.Id, "cw", "mp");

                            if (newData != null)
                            {
                                List<string> output = new List<string>();
                                output.Add("```md\nBLACK OPS COLD WAR LIFETIME KILLS\n=================================```");

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
                                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills in your lifetime out of all Black Ops Cold War participants!", newData[0].DiscordID));

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
        [Command("bocw participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw participants"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            ulong serverID = Context.Guild.Id;
                            string gameAbbrev = "cw";
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
        [Command("bocw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw add participant"))
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
                                string gameAbbrev = "cw";
                                string modeAbbrev = "mp";

                                if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been added to the Black Ops Cold War participant list.", discordID));
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
        [Command("bocw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw rm participant"))
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
                                string gameAbbrev = "cw";
                                string modeAbbrev = "mp";

                                if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
                                    await ReplyAsync(string.Format("<@!{0}> has been removed from the Black Ops Cold War participant list.", discordID));
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

        [Command("bocw participate", RunMode = RunMode.Async)]
        public async Task ParticipateCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw participate"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "cw";
                            string modeAbbrev = "mp";

                            if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been added to the Black Ops Cold War participant list.", discordID));
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

        [Command("bocw leave", RunMode = RunMode.Async)]
        public async Task LeaveCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.BlackOpsColdWarComponent, "bocw leave"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        try
                        {
                            ulong serverID = Context.Guild.Id;
                            ulong discordID = Context.User.Id;
                            string gameAbbrev = "cw";
                            string modeAbbrev = "mp";

                            if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
                                await ReplyAsync(string.Format("<@!{0}> has been removed from the Black Ops Cold War participant list.", discordID));
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
