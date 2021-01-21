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
    public class WarzoneCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService _service;
        private AnnouncementsService _announcementsService;
        private CgBotContext _db;

        static bool handlersSet = false;

        public WarzoneCommands(IServiceProvider services)
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
            if (DisableIfServiceNotRunning(_service.WarzoneComponent))
            {
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("mw", "wz");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        await channel.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S WINNERS!!!! CONGRATULATIONS!!!\n```");

                        // pass true to keep track of lifetime total kills every week
                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(true, serverId, "mw", "wz");

                        SocketGuild guild = _service._client.GetGuild(serverId);

                        List<string> output = new List<string>();
                        output.AddRange(await GetLast7DaysKills(newData, guild));
                        output.AddRange(await GetWarzoneWins(newData, guild, true));

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
            if (DisableIfServiceNotRunning(_service.WarzoneComponent))
            {
                List<ulong> serverIds = await _service.GetAllValidatedServerIds("mw", "wz");
                foreach (ulong serverId in serverIds)
                {
                    var channel = await _service.GetServerCallOfDutyNotificationChannel(serverId);

                    if (channel != null)
                    {
                        await channel.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S CURRENT RANKINGS!\n```");

                        List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, serverId, "mw", "wz");

                        SocketGuild guild = _service._client.GetGuild(serverId);

                        List<string> output = await GetWarzoneWins(newData, guild);

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
                output.Add("```md\nWARZONE KILLS IN LAST 7 DAYS\n============================```");
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

                    ulong roleID = await _service.GetServerWarzoneKillsRoleID(guild.Id);
                    string roleStr = "";
                    if (roleID != 0)
                    {
                        await GiveUsersRole(roleID, topPlayersDiscordIDs, guild);
                        await UnassignRoleFromAllMembers(roleID, guild);
                        roleStr = $" You have been assigned the role <@&{roleID}>!";
                    }

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@" <@!{0}>,", DiscordID);
                    }

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations{0} you have the most Warzone kills out of all Warzone participants in the last 7 days!{1}", winners, roleStr));
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

        public async Task<List<string>> GetWarzoneWins(List<CallOfDutyPlayerModel> newData, SocketGuild guild = null, bool updateRoles = false)
        {
            if (guild == null)
                guild = Context.Guild;

            List<string> output = new List<string>();

            if (newData != null)
            {
                output.Add("```md\nWARZONE WINS\n============```");
                newData = newData.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();

                int playerCount = 1;
                bool atleastOnePlayer = false;
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

                    output = ValidateOutputLimit(output, string.Format(@"**{0}.)** <@!{1}> has {2} total Warzone wins.", playerCount, player.DiscordID, wins) + "\n");
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

                    output = ValidateOutputLimit(output, "\n" + $"{winners}you have the most Warzone wins out of all Warzone participants!");

                    // update the roles only every week
                    ulong roleID = await _service.GetServerWarzoneWinsRoleID(guild.Id);
                    if (updateRoles && roleID != 0)
                    {
                        await UnassignRoleFromAllMembers(roleID, guild);
                        await GiveUsersRole(roleID, topPlayersDiscordIDs, guild);

                        output = ValidateOutputLimit(output, $" You have been assigned the role <@&{roleID}>!");
                    }
                }
                else
                    output = ValidateOutputLimit(output, "\n" + "No active players.");

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
        [Command("wz wins", RunMode = RunMode.Async)]
        public async Task WinsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.WarzoneComponent, "wz wins"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_service._db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            List<CallOfDutyPlayerModel> newData = await _service.GetNewPlayerData(false, Context.Guild.Id, "mw", "wz");

                            List<string> output = await GetWarzoneWins(newData);
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
        [Command("wz participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz participants"))
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
                            string modeAbbrev = "wz";

                            await ListPartcipants(_service, serverID, gameAbbrev, modeAbbrev);
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
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz add participant"))
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
                                string modeAbbrev = "wz";

                                if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
                {
                    if (DisableIfServiceNotRunning(_service.ModernWarfareComponent, "wz rm participant"))
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
                                string modeAbbrev = "wz";

                                if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
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

                            if (await AddAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
                if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
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

                            if (await RemoveAParticipant(_service, serverID, discordID, gameAbbrev, modeAbbrev))
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
