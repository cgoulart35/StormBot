using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using cg_bot.Services;
using cg_bot.Models.CallOfDutyModels.Players;
using cg_bot.Models.CallOfDutyModels.Players.Data;
using Discord;

namespace cg_bot.Modules.CallOfDutyModules
{
    public class ModernWarfareCommands : BaseCallOfDutyCommands
    {
        private CallOfDutyService<ModernWarfareDataModel> _service;

        public ModernWarfareCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
        }

        [Command("mw kills", RunMode = RunMode.Async)]
        public async Task KillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw kills"))
            {
                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "__**Modern Warfare Kills**__\n";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                    {
                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, player.Data.Lifetime.All.Properties.Kills) + "\n";
                        playerCount++;
                    }

                    await UnassignRoleFromAllUsers(Program.configurationSettingsModel.ModernWarfareKillsRoleID);
                    await GiveUserRole(Program.configurationSettingsModel.ModernWarfareKillsRoleID, newData.Players[0].DiscordID);

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills out of all Modern Warfare participants! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.ModernWarfareKillsRoleID);

                    await ReplyAsync(output);
                }
                else
                {
                    await ReplyAsync("No data returned.");
                }
            }
        }

        [Command("mw wz wins", RunMode = RunMode.Async)]
        public async Task WinsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "mw wz wins"))
            {
                CallOfDutyAllPlayersModel<ModernWarfareDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "__**Modern Warfare Warzone Wins**__\n";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<ModernWarfareDataModel> player in newData.Players)
                    {
                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total Warzone wins.", playerCount, player.DiscordID, player.Data.Lifetime.Mode.BattleRoyal.Properties.Wins) + "\n";
                        playerCount++;
                    }

                    await UnassignRoleFromAllUsers(Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID);
                    await GiveUserRole(Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID, newData.Players[0].DiscordID);

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most Warzone wins out of all Modern Warfare participants! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID);

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
                await ListPartcipants(_service);
            }
        }

        [Command("mw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "mw add participant"))
            {
                string input = GetSingleArg(args);
                string trimmedInput = input.Substring(3, 18);
                ulong discordID = Convert.ToUInt64(trimmedInput);
                if (await AddAParticipant(_service, discordID))
                    await ReplyAsync(string.Format("<@!{0}> has been added to the Modern Warfare participant list.", discordID));
            }
        }

        [Command("mw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "mw rm participant"))
            {
                string input = GetSingleArg(args);
                string trimmedInput = input.Substring(3, 18);
                ulong discordID = Convert.ToUInt64(trimmedInput);
                if (await RemoveAParticipant(_service, discordID))
                    await ReplyAsync(string.Format("<@!{0}> has been removed from the Modern Warfare participant list.", discordID));
            }
        }
    }
}
