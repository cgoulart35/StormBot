using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cg_bot.Models.CallOfDutyModels.Accounts;
using cg_bot.Services;
using Discord.WebSocket;

namespace cg_bot.Modules.CallOfDutyModules
{
	public class BaseCallOfDutyCommands : BaseCommand
	{
        public async Task UnassignRoleFromAllMembers(ulong roleID, SocketGuild guild)
        {
            var role = guild.GetRole(roleID);
            IEnumerable<SocketGuildUser> roleMembers = guild.GetRole(roleID).Members;
            foreach (SocketGuildUser roleMember in roleMembers)
            {
                await roleMember.RemoveRoleAsync(role);
            }
        }

        public async Task GiveUserRole(ulong roleID, ulong discordID, SocketGuild guild)
        {
            var role = guild.GetRole(roleID);
            var roleMember = guild.GetUser(discordID);

            await roleMember.AddRoleAsync(role);
        }

        public async Task<bool> AddAParticipant<T>(CallOfDutyService<T> service, ulong discordID)
        {
            CallOfDutyAccountModel account = new CallOfDutyAccountModel();

            account.DiscordID = discordID;

            await ReplyAsync(string.Format("What is <@!{0}>'s Call of Duty username? Capitalization matters. Do not include the '#number' tag after the name. (on Battle.net, PlayStation, Xbox, Steam, Activision)", discordID));
            account.Username = await PromptUserForStringForPartcipant();

            if (account.Username == "cancel")
                return false;

            await ReplyAsync(string.Format("What is <@!{0}>'s Call of Duty username's tag? If there is no tag, say 'none'. Do not include the '#' symbol in your answer. (Example: 1234 in User#1234)", discordID));
            account.Tag = await PromptUserForStringForPartcipant(true);

            if (account.Tag == "cancel")
                return false;

            account.Platform = await AskPlatform(discordID);

            if (account.Platform == "cancel")
                return false;

            service.AddParticipantToFile(account);
            return true;
        }

        public async Task<bool> RemoveAParticipant<T>(CallOfDutyService<T> service, ulong discordID)
        {
            CallOfDutyAllAccountsModel participatingAccountsData = service.ReadParticipatingAccounts();

            if (participatingAccountsData.Accounts.Count != 0)
            {
                foreach (CallOfDutyAccountModel account in participatingAccountsData.Accounts)
                {
                    if (account.DiscordID == discordID)
                    {
                        service.RemoveParticipantFromFile(account);
                        return true;
                    }
                }

                await ReplyAsync("This user isn't participating.");
                return false;
            }
            else
            {
                return false;
            }
        }

        public async Task<string> PromptUserForStringForPartcipant(bool forTag = false)
        {
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 1, 0));

            string requestedString = userSelectResponse.Content;

            // if user responds in time
            if (userSelectResponse != null)
            {
                if (forTag && requestedString == "none")
                {
                    return "";
                }
                // if response is cancel, don't add participant
                if (requestedString.ToLower() == "cancel")
                {
                    await ReplyAsync("Request cancelled.");
                    return "cancel";
                }
                // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                else if (requestedString.StartsWith(Program.configurationSettingsModel.Prefix))
                {
                    return "cancel";
                }
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
                return "cancel";
            }

            return requestedString;
        }

        public async Task<string> AskPlatform(ulong discordID)
        {
            string platforms = "**1.)** Battle.net\n**2.)** PlayStation\n**3.)** Xbox\n**4.)** Steam\n**5.)** Activision\n";

            await ReplyAsync(string.Format("What is <@!{0}>'s Call of Duty username's game platform? Please respond with the corresponding number:\n", discordID) + platforms);
            int selection = await PromptUserForNumber(5);

            switch (selection)
            {
                case (-1):
                    return "cancel";
                case (1):
                    return "battle";
                case (2):
                    return "psn";
                case (3):
                    return "xbl";
                case (4):
                    return "steam";
                default:
                    return "uno";
            }
        }

        public async Task<int> PromptUserForNumber(int maxSelection)
        {
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 1, 0));

            string username = Context.User.Username;

            // if user responds in time
            if (userSelectResponse != null)
            {
                string requestedNumber = userSelectResponse.Content;

                // if response is not a number
                if (!(int.TryParse(requestedNumber, out int validatedNumber)))
                {
                    // if response is cancel, don't remove
                    if (requestedNumber.ToLower() == "cancel")
                    {
                        await ReplyAsync("Request cancelled.");
                        return -1;
                    }
                    // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                    else if (requestedNumber.StartsWith(Program.configurationSettingsModel.Prefix))
                    {
                        return -1;
                    }
                    // if not cancel, request another response
                    else
                    {
                        await ReplyAsync($"{username}, your response was invalid. Please answer with a number.");
                        return await PromptUserForNumber(maxSelection);
                    }
                }
                // if response is a number
                else
                {
                    // if number is valid option on list of sounds
                    if (validatedNumber >= 1 && validatedNumber <= maxSelection)
                    {
                        await ReplyAsync($"{username} entered: {validatedNumber}");
                        return validatedNumber;
                    }
                    // if not valid number, request another response
                    else
                    {
                        await ReplyAsync($"{username}, your response was invalid. Please answer a number shown on the list.");
                        return await PromptUserForNumber(maxSelection);
                    }
                }
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
                return -1;
            }
        }

        public async Task<CallOfDutyAllAccountsModel> ListPartcipants<T>(CallOfDutyService<T> service)
        {
            CallOfDutyAllAccountsModel participatingAccountsData = service.ReadParticipatingAccounts();
            string output = "__**Participants: " + service._dataModel.GameName + "**__\n";

            if (participatingAccountsData.Accounts.Count != 0)
            {
                int accountCount = 1;
                foreach (CallOfDutyAccountModel account in participatingAccountsData.Accounts)
                {
                    ulong discordID = account.DiscordID;
                    string username = account.Username;
                    string tag = "";
                    string platform = "";

                    if (account.Tag != "")
                        tag = "#" + account.Tag;

                    if (account.Platform == "battle")
                        platform = "Battle.net";
                    else if (account.Platform == "steam")
                        platform = "Steam";
                    else if (account.Platform == "psn")
                        platform = "PlayStation";
                    else if (account.Platform == "xbl")
                        platform = "Xbox";
                    else if (account.Platform == "uno")
                        platform = "Activision";

                    output += string.Format(@"**{0}.)** <@!{1}> ({2}{3}, {4}).", accountCount, discordID, username, tag, platform) + "\n";

                    accountCount++;
                }
                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("Zero participants.");
            }
            return participatingAccountsData;
        }
    }
}
