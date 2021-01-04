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
	public class ModernWarfareCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService<ModernWarfareDataModel> _service;
        private AnnouncementsService _announcementsService;

        static bool handlersSet = false;

        public ModernWarfareCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
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
                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData(true);

                List<string> output = new List<string>();
                output.AddRange(await GetLast7DaysKills(newData, guild));

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

                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                List<string> output = new List<string>();
                output.AddRange(GetWeeklyKills(newData, guild));

                if (output[0] != "")
                {
                    foreach (string chunk in output)
                    {
                        await _service._callOfDutyNotificationChannelID.SendMessageAsync(chunk);
                    }
                }
            }
        }

        public async Task<List<string>> GetLast7DaysKills(CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            List<string> output = new List<string>();

            if (newData != null)
            {
                output.Add("```md\nMODERN WARFARE KILLS IN LAST 7 DAYS\n===================================```");
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();

                int playerCount = 1;
                bool atleastOnePlayer = false;
                foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
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
                    await UnassignRoleFromAllMembers(Program.configurationSettingsModel.ModernWarfareKillsRoleID, guild);
                    await GiveUserRole(Program.configurationSettingsModel.ModernWarfareKillsRoleID, newData.Players[0].DiscordID, guild);

                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations <@!{0}>, you have the most Modern Warfare kills out of all Modern Warfare participants in the last 7 days! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.ModernWarfareKillsRoleID));
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

        public List<string> GetWeeklyKills(CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData, SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            CallOfDutyAllPlayersModel<ModernWarfareDataModel> storedData = _service.storedPlayerDataModel;
            List<CallOfDutyPlayerModel<ModernWarfareDataModel>> outputPlayers = new List<CallOfDutyPlayerModel<ModernWarfareDataModel>>();

            List<string> output = new List<string>();

            if (newData != null && storedData != null)
            {
                output.Add("```md\nMODERN WARFARE & WARZONE WEEKLY KILLS\n=====================================```");

                // set weekly kill counts
                foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                {
                    CallOfDutyPlayerModel<ModernWarfareDataModel> outputPlayer = player;

                    double kills = 0;

                    // if user has played at all
                    if (player.Data.Lifetime.All.Properties != null)
                    {
                        // if player kill count saved last week, set kills this week
                        if (storedData.Players.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID) != null)
                            kills = player.Data.Lifetime.All.Properties.Kills - storedData.Players.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID).Data.Lifetime.All.Properties.Kills;
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
                foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in outputPlayers)
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
                    output = ValidateOutputLimit(output, "\n" + string.Format(@"Looks like <@!{0}> is currently in the lead with the most Modern Warfare + Warzone kills this week!", outputPlayers[0].DiscordID));
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

        [Command("mw wz weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw wz weekly kills"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                List<string> output = GetWeeklyKills(newData);
                if (output[0] != "")
                {
                    foreach (string chunk in output)
                    {
                        await ReplyAsync(chunk);
                    }
                }
            }
        }

        [Command("mw wz lifetime kills", RunMode = RunMode.Async)]
        public async Task LifetimeKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw wz lifetime kills"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    List<string> output = new List<string>();
                    output.Add("```md\nMODERN WARFARE & WARZONE LIFETIME KILLS\n=======================================```");

                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    bool atleastOnePlayer = false;
                    foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
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
                        output = ValidateOutputLimit(output, "\n" + string.Format(@"Congratulations <@!{0}>, you have the most Modern Warfare + Warzone kills in your lifetime out of all Modern Warfare participants!", newData.Players[0].DiscordID));

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

                    ulong discordID = GetDiscordUserID(input);

                    if (await AddAParticipant(_service, discordID))
                        await ReplyAsync(string.Format("<@!{0}> has been added to the Modern Warfare & Warzone participant list.", discordID));
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

                    ulong discordID = GetDiscordUserID(input);

                    if (await RemoveAParticipant(_service, discordID))
                        await ReplyAsync(string.Format("<@!{0}> has been removed from the Modern Warfare & Warzone participant list.", discordID));
                }
                catch
                {
                    await ReplyAsync("Please provide a valid Discord user.");
                }
            }
        }
    }
}
