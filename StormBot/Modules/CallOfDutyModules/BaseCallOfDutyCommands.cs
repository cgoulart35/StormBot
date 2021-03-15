using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using StormBot.Services;
using StormBot.Database.Entities;

namespace StormBot.Modules.CallOfDutyModules
{
	public class BaseCallOfDutyCommands : BaseCommand
	{
        public async Task<bool> AddAParticipant(ulong serverID, ulong discordID, string gameAbbrev, string modeAbbrev)
        {
            CallOfDutyPlayerDataEntity newAccount = new CallOfDutyPlayerDataEntity();

            newAccount.ServerID = serverID;
            newAccount.DiscordID = discordID;
            newAccount.GameAbbrev = gameAbbrev;
            newAccount.ModeAbbrev = modeAbbrev;

            await Context.User.SendMessageAsync(string.Format("What is <@!{0}>'s Call of Duty username? Capitalization matters. Do not include the '#number' tag after the name. (on Battle.net, PlayStation, Xbox, Steam, Activision)", discordID));
            newAccount.Username = await PromptUserForStringForPartcipant();

            if (newAccount.Username == "cancel")
                return false;

            await Context.User.SendMessageAsync(string.Format("What is <@!{0}>'s Call of Duty username's tag? If there is no tag, say 'none'. Do not include the '#' symbol in your answer. (Example: 1234 in User#1234)", discordID));
            newAccount.Tag = await PromptUserForStringForPartcipant(true);

            if (newAccount.Tag == "cancel")
                return false;

            newAccount.Platform = await AskPlatform(discordID);

            if (newAccount.Platform == "cancel")
                return false;

			CallOfDutyService.AddParticipantToDatabase(newAccount);
            
            return true;
        }

        public async Task<bool> RemoveAParticipant(ulong serverID, ulong discordID, string gameAbbrev, string modeAbbrev)
        {
            CallOfDutyPlayerDataEntity removeAccount = CallOfDutyService.GetCallOfDutyPlayerDataEntity(serverID, discordID, gameAbbrev, modeAbbrev);

            if (removeAccount != null)
            {
				CallOfDutyService.RemoveParticipantFromDatabase(removeAccount);

                return true;
            }
            else
            {
                await ReplyAsync("This user isn't participating.");
                return false;
            }
        }

        public async Task<string> PromptUserForStringForPartcipant(bool forTag = false)
        {
            var userSelectResponse = await NextMessageAsync(true, false, new TimeSpan(0, 1, 0));

            string requestedString = null;

            // if user responds in time
            if (userSelectResponse != null)
            {
                requestedString = userSelectResponse.Content;

                if (forTag && requestedString == "none")
                {
                    return "";
                }
                // if response is cancel, don't add participant
                if (requestedString.ToLower() == "cancel")
                {
                    await Context.User.SendMessageAsync("Request cancelled.");
                    return "cancel";
                }
                // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                else if (requestedString.StartsWith(BaseService.GetServerOrPrivateMessagePrefix(Context)))
                {
                    return "cancel";
                }
            }
            // if user doesn't respond in time
            else
            {
                await Context.User.SendMessageAsync("You did not reply before the timeout.");
                return "cancel";
            }

            return requestedString;
        }

        public async Task<string> AskPlatform(ulong discordID)
        {
            string platforms = "**1.)** Battle.net\n**2.)** PlayStation\n**3.)** Xbox\n**4.)** Steam\n**5.)** Activision\n";

            await Context.User.SendMessageAsync(string.Format("What is <@!{0}>'s Call of Duty username's game platform? Please respond with the corresponding number:\n", discordID) + platforms);
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
            var userSelectResponse = await NextMessageAsync(true, false, new TimeSpan(0, 1, 0));

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
                        await Context.User.SendMessageAsync("Request cancelled.");
                        return -1;
                    }
                    // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                    else if (requestedNumber.StartsWith(BaseService.GetServerOrPrivateMessagePrefix(Context)))
                    {
                        return -1;
                    }
                    // if not cancel, request another response
                    else
                    {
                        await Context.User.SendMessageAsync($"{username}, your response was invalid. Please answer with a number.");
                        return await PromptUserForNumber(maxSelection);
                    }
                }
                // if response is a number
                else
                {
                    // if number is valid option on list of sounds
                    if (validatedNumber >= 1 && validatedNumber <= maxSelection)
                    {
                        await Context.User.SendMessageAsync($"{username} entered: {validatedNumber}");
                        return validatedNumber;
                    }
                    // if not valid number, request another response
                    else
                    {
                        await Context.User.SendMessageAsync($"{username}, your response was invalid. Please answer a number shown on the list.");
                        return await PromptUserForNumber(maxSelection);
                    }
                }
            }
            // if user doesn't respond in time
            else
            {
                await Context.User.SendMessageAsync("You did not reply before the timeout.");
                return -1;
            }
        }

        public async Task<List<CallOfDutyPlayerDataEntity>> ListPartcipants(ulong serverId, string gameAbbrev, string modeAbbrev)
        {
            List<ulong> serverIdList = new List<ulong>();
            serverIdList.Add(serverId);

            List<CallOfDutyPlayerDataEntity> participatingAccountsData = CallOfDutyService.GetServersPlayerData(serverIdList, gameAbbrev, modeAbbrev);

            string gameName = "";
            if (gameAbbrev == "mw" && modeAbbrev == "mp")
                gameName = "Modern Warfare";
            else if (gameAbbrev == "mw" && modeAbbrev == "wz")
                gameName = "Warzone";
            else if (gameAbbrev == "cw" && modeAbbrev == "mp")
                gameName = "Black Ops Cold War";

            string output = "__**Participants: " + gameName + "**__\n";

            if (participatingAccountsData.Any())
            {
                int accountCount = 1;
                foreach (CallOfDutyPlayerDataEntity account in participatingAccountsData)
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
