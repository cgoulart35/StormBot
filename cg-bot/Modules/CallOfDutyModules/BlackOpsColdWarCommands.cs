using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
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

        [Command("bocw kills", RunMode = RunMode.Async)]
        public async Task KillsCommand()
        {
            if (DisableIfServiceNotRunning(_service, "bocw kills"))
            {
                CallOfDutyAllPlayersModel<BlackOpsColdWarDataModel> newData = _service.GetNewPlayerData();

                if (newData != null)
                {
                    string output = "__** Black Ops Cold War Kills**__\n";
                    newData.Players = newData.Players.OrderByDescending(player => player.Data.Lifetime.All.Properties.Kills).ToList();

                    int playerCount = 1;
                    foreach (CallOfDutyPlayerModel<BlackOpsColdWarDataModel> player in newData.Players)
                    {
                        output += string.Format(@"**{0}.)** <@!{1}> has {2} total game kills.", playerCount, player.DiscordID, player.Data.Lifetime.All.Properties.Kills) + "\n";
                        playerCount++;
                    }

                    await UnassignRoleFromAllUsers(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID);
                    await GiveUserRole(Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID, newData.Players[0].DiscordID);

                    output += "\n" + string.Format(@"Congratulations <@!{0}>, you have the most kills out of all Black Ops Cold War participants! You have been assigned the role <@&{1}>!", newData.Players[0].DiscordID, Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID);

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
                await ListPartcipants(_service);
            }
        }

        [Command("bocw add participant", RunMode = RunMode.Async)]
        public async Task AddParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "bocw add participant"))
            {
                string input = GetSingleArg(args);
                string trimmedInput = input.Substring(3, 18);
                ulong discordID = Convert.ToUInt64(trimmedInput);
                if (await AddAParticipant(_service, discordID))
                    await ReplyAsync(string.Format("<@!{0}> has been added to the Black Ops Cold War participant list.", discordID));
            }
        }

        [Command("bocw rm participant", RunMode = RunMode.Async)]
        public async Task RemoveParticipantCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_service, "bocw rm participant"))
            {
                string input = GetSingleArg(args);
                string trimmedInput = input.Substring(3, 18);
                ulong discordID = Convert.ToUInt64(trimmedInput);
                if (await RemoveAParticipant(_service, discordID))
                    await ReplyAsync(string.Format("<@!{0}> has been removed from the Black Ops Cold War participant list.", discordID));
            }
        }
    }
}
