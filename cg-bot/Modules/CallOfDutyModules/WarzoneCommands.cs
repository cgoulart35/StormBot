using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using cg_bot.Services;
using cg_bot.Models.CallOfDutyModels.Players;
using cg_bot.Models.CallOfDutyModels.Players.Data;

namespace cg_bot.Modules.CallOfDutyModules
{
    public class WarzoneCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService<WarzoneDataModel> _service;
        private AnnouncementsService _announcementsService;

        static bool handlersSet = false;

        public WarzoneCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<WarzoneDataModel>>();
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
            if (DisableIfServiceNotRunning(_service))
            {
                SocketGuild guild = _service._client.Guilds.First();

                // pass true to keep track of lifetime total kills every week
                CallOfDutyAllPlayersModel<WarzoneDataModel> newData = _service.GetNewPlayerData(true);

                List<string> output = new List<string>();
                output.AddRange(await GetLast7DaysKills(newData, guild));
                output.AddRange(await GetWarzoneWins(newData, guild, true));

                if (output[0] != "")
                {
                    foreach (string chunk in output)
                    {
                        await _service._callOfDutyNotificationChannelID.SendMessageAsync(chunk);
                    }
                }
            }
        }

        public async Task DailyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service))
            {
                SocketGuild guild = _service._client.Guilds.First();

                CallOfDutyAllPlayersModel<WarzoneDataModel> newData = _service.GetNewPlayerData();

                List<string> output = new List<string>();
                output.AddRange(await GetWarzoneWins(newData, guild));

                if (output[0] != "")
                {
                    foreach (string chunk in output)
                    {
                        await _service._callOfDutyNotificationChannelID.SendMessageAsync(chunk);
                    }
                }
            }
        }

        public async Task<List<string>> GetLast7DaysKills(CallOfDutyAllPlayersModel<WarzoneDataModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            List<string> output = new List<string>();

            if (newData != null)
            {
                output.Add("```md\nWARZONE KILLS IN LAST 7 DAYS\n============================```");
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();

                int playerCount = 1;
                bool atleastOnePlayer = false;
                foreach (CallOfDutyPlayerModel<WarzoneDataModel> player in newData.Players)
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
                    await UnassignRoleFromAllMembers(Program.configurationSettingsModel.WarzoneKillsRoleID, guild);

                    double topScore = newData.Players[0].Data.Weekly.All.Properties.Kills;
                    List<ulong> topPlayersDiscordIDs = newData.Players.Where(player => player.Data.Weekly.All.Properties?.Kills == topScore).Select(player => player.DiscordID).ToList();
                    await GiveUsersRole(Program.configurationSettingsModel.WarzoneKillsRoleID, topPlayersDiscordIDs, guild);

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@" <@!{0}>,", DiscordID);
                    }

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations{0} you have the most Warzone kills out of all Warzone participants in the last 7 days! You have been assigned the role <@&{1}>!", winners, Program.configurationSettingsModel.WarzoneKillsRoleID));
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

        public async Task<List<string>> GetWarzoneWins(CallOfDutyAllPlayersModel<WarzoneDataModel> newData, SocketGuild guild = null, bool updateRoles = false)
        {
            if (guild == null)
                guild = Context.Guild;

            List<string> output = new List<string>();

            if (newData != null)
            {
                output.Add("```md\nWARZONE WINS\n============```");
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();

                int playerCount = 1;
                bool atleastOnePlayer = false;
                foreach (CallOfDutyPlayerModel<WarzoneDataModel> player in newData.Players)
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
                    double topScore = newData.Players[0].Data.Lifetime.Mode.BattleRoyal.Properties.Wins;
                    List<ulong> topPlayersDiscordIDs = newData.Players.Where(player => player.Data.Lifetime.Mode.BattleRoyal.Properties?.Wins == topScore).Select(player => player.DiscordID).ToList();

                    string winners = "";
                    foreach (ulong DiscordID in topPlayersDiscordIDs)
                    {
                        winners += string.Format(@"<@!{0}>, ", DiscordID);
                    }

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"{0}you have the most Warzone wins out of all Warzone participants!", winners));

                    // update the roles only every week
                    if (updateRoles)
                    {
                        await UnassignRoleFromAllMembers(Program.configurationSettingsModel.WarzoneWinsRoleID, guild);
                        await GiveUsersRole(Program.configurationSettingsModel.WarzoneWinsRoleID, topPlayersDiscordIDs, guild);

                        output = ValidateOutputLimit(output, string.Format(" Congratulations, you have been assigned the role <@&{0}>!", Program.configurationSettingsModel.WarzoneWinsRoleID));
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

        [Command("wz wins", RunMode = RunMode.Async)]
        public async Task WinsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "wz wins"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<WarzoneDataModel> newData = _service.GetNewPlayerData();

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
