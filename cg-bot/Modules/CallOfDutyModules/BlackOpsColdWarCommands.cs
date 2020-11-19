using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord;
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

        public BlackOpsColdWarCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<BlackOpsColdWarDataModel>>();
        }

        [Command("bocw weekly kills", RunMode = RunMode.Async)]
        public async Task WeeklyKillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "bocw weekly kills"))
            {
                await Context.Channel.TriggerTypingAsync();

                CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "__** Black Ops Cold War Weekly Kills**__\n";
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

                        output += string.Format(@"**{0}.)** <@!{1}> has {2} kills this week.", playerCount, player.DiscordID, kills) + "\n";
                        playerCount++;
                    }

                    await UnassignRoleFromAllMembers(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID);
                    await GiveUserRole(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID, newData.Players[0].DiscordID);

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills out of all Black Ops Cold War participants this week! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID);

                    await ReplyAsync(output);
                }
                else
                {
                    await ReplyAsync("No data returned.");
                }
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
                    string output = "__** Black Ops Cold War Lifetime Kills**__\n";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in newData.Players)
                    {
                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, player.Data.Lifetime.All.Properties.Kills) + "\n";
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
