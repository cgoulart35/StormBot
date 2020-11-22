using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using cg_bot.Services;
using cg_bot.Models.CallOfDutyModels.Players;
using cg_bot.Models.CallOfDutyModels.Players.Data;

namespace cg_bot.Modules.CallOfDutyModules
{
	public class ModernWarfareCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService<ModernWarfareDataModel> _service;
        private AnnouncementsService _announcementsService;

        public ModernWarfareCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
            _announcementsService = services.GetRequiredService<AnnouncementsService>();

            _announcementsService.CallOfDutyAnnouncement += WeeklyCompetitionUpdates;
        }

        public async Task WeeklyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service))
            {
                SocketGuild guild = _service._client.Guilds.First();
                string output = "";
                output += await GetWeeklyKills(guild) + "\n";
                output += await GetWeeklyWins(guild);
                await _service._callOfDutyNotificationChannelID.SendMessageAsync(output);
            }
        }

        public async Task<string> GetWeeklyKills(SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

            if (newData != null)
            {
                string output = "__**Modern Warfare Weekly Kills**__\n";
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();

                int playerCount = 1;
                foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                {
                    double kills;

                    // if user has played this week
                    if (player.Data.Weekly.All.Properties == null)
                        kills = 0;
                    // if user has not played this week
                    else
                        kills = player.Data.Weekly.All.Properties.Kills;

                    output += string.Format(@"**{0}.)** <@!{1}> has {2} kills this week.", playerCount, player.DiscordID, kills) + "\n";
                    playerCount++;
                }

                await UnassignRoleFromAllMembers(Program.configurationSettingsModel.ModernWarfareKillsRoleID, guild);
                await GiveUserRole(Program.configurationSettingsModel.ModernWarfareKillsRoleID, newData.Players[0].DiscordID, guild);

                output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills out of all Modern Warfare participants this week! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.ModernWarfareKillsRoleID);
                return output;
            }
            else
                return "No data returned.";
        }
        
        public async Task<string> GetWeeklyWins(SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

            if (newData != null)
            {
                string output = "__**Modern Warfare Warzone Wins**__\n";
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();

                int playerCount = 1;
                foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                {
                    double wins = 0;

                    // if user has played
                    if (player.Data.Lifetime.All.Properties == null)
                        wins = 0;
                    // if user has not played
                    else
                        wins = player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins;

                    output += string.Format(@"**{0}.)** <@!{1}> has {2} total Warzone wins.", playerCount, player.DiscordID, wins) + "\n";
                    playerCount++;
                }

                await UnassignRoleFromAllMembers(Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID, guild);
                await GiveUserRole(Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID, newData.Players[0].DiscordID, guild);

                output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most Warzone wins out of all Modern Warfare participants! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID);

                return output;
            }
            else
                return "No data returned.";
        }

        [Command("mw weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw weekly kills"))
            {
                await Context.Channel.TriggerTypingAsync();
                await ReplyAsync(await GetWeeklyKills());
            }
        }

        [Command("mw wz wins", RunMode = RunMode.Async)]
        public async Task WinsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw wz wins"))
            {
                await Context.Channel.TriggerTypingAsync();
                await ReplyAsync(await GetWeeklyWins());
            }
        }

        [Command("mw lifetime kills", RunMode = RunMode.Async)]
        public async Task LifetimeKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw lifetime kills"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "__**Modern Warfare Lifetime Kills**__\n";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                    {
                        double kills = 0;

                        // if user has played
                        if (player.Data.Lifetime.All.Properties == null)
                            kills = 0;
                        // if user has not played
                        else
                            kills = player.Data.Lifetime.All.Properties.Kills;

                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, kills) + "\n";
                        playerCount++;
                    }

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills in your lifetime out of all Modern Warfare participants!", newData.Players[0].DiscordID);

                    await ReplyAsync(output);
                }
                else
                {
                    await ReplyAsync("No data returned.");
                }
            }
        }

        [Command("mw participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw participants"))
            {
                await Context.Channel.TriggerTypingAsync();

                await ListPartcipants(_service);
            }
        }

        [Command("mw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "mw add participant"))
            {
                await Context.Channel.TriggerTypingAsync();

                try
                {
                    string input = GetSingleArg(args);

                    // if no name given
                    if (input == null)
                        throw new Exception();

                    string trimmedInput = input.Substring(3, 18);
                    ulong discordID = Convert.ToUInt64(trimmedInput);

                    // if user exists in the server
                    if (Context.Guild.GetUser(discordID) == null)
                        throw new Exception();

                    if (await AddAParticipant(_service, discordID))
                        await ReplyAsync(string.Format("<@!{0}> has been added to the Modern Warfare participant list.", discordID));
                }
                catch
                {
                    await ReplyAsync("Please provide a valid Discord user.");
                }
            }
        }

        [Command("mw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "mw rm participant"))
            {
                await Context.Channel.TriggerTypingAsync();

                try
                {
                    string input = GetSingleArg(args);

                    // if no name given
                    if (input == null)
                        throw new Exception();

                    string trimmedInput = input.Substring(3, 18);
                    ulong discordID = Convert.ToUInt64(trimmedInput);

                    // if user exists in the server
                    if (Context.Guild.GetUser(discordID) == null)
                        throw new Exception();

                    if (await RemoveAParticipant(_service, discordID))
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
