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
	public class BlackOpsColdWarCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService<BlackOpsColdWarDataModel> _service;
        private AnnouncementsService _announcementsService;

        public BlackOpsColdWarCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<BlackOpsColdWarDataModel>>();
            _announcementsService = services.GetRequiredService<AnnouncementsService>();

            _announcementsService.WeeklyCallOfDutyAnnouncement += WeeklyCompetitionUpdates;
            _announcementsService.DailyCallOfDutyAnnouncement += DailyCompetitionUpdates;
        }

        public async Task WeeklyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service))
            {
                SocketGuild guild = _service._client.Guilds.First();
                string output = "";
                output += await GetLast7DaysKills(guild);
                await _service._callOfDutyNotificationChannelID.SendMessageAsync(output);
            }
        }

        public async Task DailyCompetitionUpdates(object sender, EventArgs args)
        {
            // if service is running, display updates
            if (DisableIfServiceNotRunning(_service))
            {
                SocketGuild guild = _service._client.Guilds.First();
                string output = "";
                output += GetWeeklyKills(guild);
                await _service._callOfDutyNotificationChannelID.SendMessageAsync(output);
            }
        }

        public async Task<string> GetLast7DaysKills(SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            // pass true to keep track of lifetime total kills every week
            CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> newData = _service.GetNewPlayerData(true);

            if (newData != null)
            {
                string output = "```md\nBLACK OPS COLD WAR KILLS IN LAST 7 DAYS\n=======================================```";
                newData.Players = newData.Players.OrderByDescending(player => player.Data.Weekly.All.Properties != null ? player.Data.Weekly.All.Properties.Kills : 0).ToList();

                int playerCount = 1;
                foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in newData.Players)
                {
                    double kills;

                    // if user has played this week
                    if (player.Data.Weekly.All.Properties == null)
                        kills = 0;
                    // if user has not played this week
                    else
                        kills = player.Data.Weekly.All.Properties.Kills;

                    output += string.Format(@"**{0}.)** <@!{1}> has {2} kills in the last 7 days.", playerCount, player.DiscordID, kills) + "\n";
                    playerCount++;
                }

                await UnassignRoleFromAllMembers(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID, guild);
                await GiveUserRole(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID, newData.Players[0].DiscordID, guild);

                output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills out of all Black Ops Cold War participants in the last 7 days! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID);

                return output;
            }
            else
            {
                return "No data returned.";
            }
        }

        public string GetWeeklyKills(SocketGuild guild = null)
        {
            if (guild == null)
                guild = Context.Guild;

            CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> storedData = _service.storedPlayerDataModel;
            CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> newData = _service.GetNewPlayerData();
            List<CallOfDutyPlayerModel<BlackOpsColdWarDataModel>> outputPlayers = new List<CallOfDutyPlayerModel<BlackOpsColdWarDataModel>>();

            if (newData != null && storedData != null)
            {
                string output = "```md\nBLACK OPS COLD WAR WEEKLY KILLS\n===============================```";

                // set weekly kill counts
                foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in newData.Players)
                {
                    CallOfDutyPlayerModel<BlackOpsColdWarDataModel> outputPlayer = player;

                    double kills = 0;

                    // if user has played at all
                    if (player.Data.Lifetime.All.Properties != null)
                    {
                        // if player kill count saved last week, set kills this week
                        if (storedData.Players.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID) != null)
                        {
                            kills = player.Data.Lifetime.All.Properties.Kills - storedData.Players.Find(storedPlayer => storedPlayer.DiscordID == player.DiscordID).Data.Lifetime.All.Properties.Kills;
                        }
                        // if player kill count not saved last week, set kills = -1
                        else
                        {
                            kills = -1;
                        }
                    }

                    outputPlayer.Data.Lifetime.All.Properties.Kills = kills;
                    outputPlayers.Add(outputPlayer);
                }

                // sort weekly kill counts
                outputPlayers = outputPlayers.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                // print weekly kills
                int playerCount = 1;
                string nextWeekMessages = "";
                foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in outputPlayers)
                {
                    if (player.Data.Lifetime.All.Properties.Kills == -1)
                        nextWeekMessages += string.Format(@"<@!{0}> will be included in daily updates starting next week.", player.DiscordID) + "\n";
                    else
                    {
                        output += string.Format(@"**{0}.)** <@!{1}> has {2} kills so far this week.", playerCount, player.DiscordID, player.Data.Lifetime.All.Properties.Kills) + "\n";
                        playerCount++;
                    }
                }

                output += nextWeekMessages;

                // never update roles here; updated only once every week
                // weekly kills at end of competition should be equal to last 7 days values from API

                output += "\n" + string.Format(@"Looks like <@!{0}> is currently in the lead with the most Black Ops Cold War kills this week!", outputPlayers[0].DiscordID);

                return output;
            }
            else
            {
                return "No data returned.";
            }
        }

        [Command("bocw weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "bocw weekly kills"))
            {
                await Context.Channel.TriggerTypingAsync();
                await ReplyAsync(GetWeeklyKills());
            }
        }

        [Command("bocw lifetime kills", RunMode = RunMode.Async)]
        public async Task LifetimeKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "bocw lifetime kills"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "```md\nBLACK OPS COLD WAR LIFETIME KILLS\n=================================```";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in newData.Players)
                    {
                        double kills = 0;

                        // if user has not played
                        if (player.Data.Lifetime.All.Properties == null)
                            kills = 0;
                        // if user has played
                        else
                            kills = player.Data.Lifetime.All.Properties.Kills;

                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, kills) + "\n";
                        playerCount++;
                    }

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills in your lifetime out of all Black Ops Cold War participants!", newData.Players[0].DiscordID);

                    await ReplyAsync(output);
                }
                else
                {
                    await ReplyAsync("No data returned.");
                }
            }
        }

        [Command("bocw participants", RunMode = RunMode.Async)]
        public async Task ParticipantsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "bocw participants"))
            {
                await Context.Channel.TriggerTypingAsync();

                await ListPartcipants(_service);
            }
        }

        [Command("bocw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "bocw add participant"))
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
                        await ReplyAsync(string.Format("<@!{0}> has been added to the Black Ops Cold War participant list.", discordID));
                }
                catch
                {
                    await ReplyAsync("Please provide a valid Discord user.");
                }
            }
        }

        [Command("bocw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "bocw rm participant"))
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
