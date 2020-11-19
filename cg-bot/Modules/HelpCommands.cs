using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cg_bot.Models.CallOfDutyModels.Players.Data;
using cg_bot.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Modules
{
    public class HelpCommands : BaseCommand
    {
        private HelpService _helpService;
        private SoundpadService _soundpadService;
        private CallOfDutyService<ModernWarfareDataModel> _modernWarfareService;
        private CallOfDutyService<BlackOpsColdWarDataModel> _blackOpsColdWarService;

        public HelpCommands(IServiceProvider services)
        {
            _helpService = services.GetRequiredService<HelpService>();
            _soundpadService = services.GetRequiredService<SoundpadService>();
            _modernWarfareService = services.GetRequiredService<CallOfDutyService<ModernWarfareDataModel>>();
            _blackOpsColdWarService = services.GetRequiredService<CallOfDutyService<BlackOpsColdWarDataModel>>();
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task HelpCommand(params string[] args)
        {
            if (DisableIfServiceNotRunning(_helpService, "help"))
            {
                await Context.Channel.TriggerTypingAsync();

                string subject = GetSingleArg(args);
                List<string> output = new List<string>();
                output.Add("");

                if (subject == null)
                {
                    output = ValidateOutputLimit(output, HelpHelpCommands());
                    output = ValidateOutputLimit(output, HelpSoundboardCommands());
                    output = ValidateOutputLimit(output, HelpModernWarfareCommands());
                    output = ValidateOutputLimit(output, HelpBlackOpsColdWarCommands());
                }
                else if (subject.ToLower() == "help")
                {
                    output = ValidateOutputLimit(output, HelpHelpCommands());
                }
                else if (subject.ToLower() == "sb" || subject.ToLower() == "sp" || subject.ToLower() == "soundboard" || subject.ToLower() == "soundpad")
                {
                    output = ValidateOutputLimit(output, HelpSoundboardCommands());
                }
                else if (subject.ToLower() == "mw" || subject.ToLower() == "modern warfare" || subject.ToLower() == "modernwarfare")
                {
                    output = ValidateOutputLimit(output, HelpModernWarfareCommands());
                }
                else if (subject.ToLower() == "bocw" || subject.ToLower() == "black ops cold war" || subject.ToLower() == "blackopscoldwar" || subject.ToLower() == "cw" || subject.ToLower() == "cold war" || subject.ToLower() == "coldwar")
                {
                    output = ValidateOutputLimit(output, HelpBlackOpsColdWarCommands());
                }
                else
                {
                    output = ValidateOutputLimit(output, "The subject name '" + subject + "' does not exist.");
                }

                if (output[0] != "")
                {
                    foreach (string chunk in output)
                    {
                        await ReplyAsync(chunk);
                    }
                }
            }
        }

        [Command("subjects", RunMode = RunMode.Async)]
        public async Task SubjectsCommand()
        {
            if (DisableIfServiceNotRunning(_helpService, "subjects"))
            {
                await Context.Channel.TriggerTypingAsync();

                string output = "__**Subjects:**__\nHelp\n";

                if (DisableIfServiceNotRunning(_soundpadService, "subjects (soundpad subject)"))
                {
                    output += "Soundpad\n";
                }
                if (DisableIfServiceNotRunning(_modernWarfareService, "subjects (Modern Warfare subject)"))
                {
                    output += "Modern Warfare\n";
                }
                if (DisableIfServiceNotRunning(_blackOpsColdWarService, "subjects (Black Ops Cold War subject)"))
                {
                    output += "Black Ops Cold War\n";
                }

                await ReplyAsync(output);
            }
        }

        private string HelpHelpCommands()
        {
            return DisableIfServiceNotRunning(_helpService, "help help") ? string.Format("\n\n" + @"__**Help: Help Commands**__

'**{0}help**' to display information on all commands.

'**{0}help [subject]**' to display information on all commands for a specific subject.

'**{0}subjects**' to display the existing command subjects.", Program.configurationSettingsModel.Prefix) : null;
        }

        private string HelpSoundboardCommands()
        {
            return DisableIfServiceNotRunning(_soundpadService, "help soundpad") ? string.Format("\n\n" + @"__**Help: Soundboard Commands**__

'**{0}add [YouTube video URL] [sound name]**' to add a YouTube to MP3 sound to the soundboard in the specified category.
The bot will then ask you to select a category to add the sound to.

'**{0}approve [user]**' to approve a user's existing request to add to the soundboard if you are an administrator.

'**{0}categories**' to display all categories.

'**{0}delete [sound number]**' to delete the sound with the corresponding number from the soundboard if you are an administrator.

'**{0}deny [user]**' to deny a user's existing request to add to the soundboard if you are an administrator.

'**{0}pause**' to pause/resume the sound currently playing.

'**{0}play [sound number]**' to play the sound with the corresponding number.

'**{0}sounds**' to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.

'**{0}sounds [category name]**' to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.

'**{0}stop**' to stop the sound currently playing.", Program.configurationSettingsModel.Prefix) : null;
        }

        private string HelpModernWarfareCommands()
        {
            return DisableIfServiceNotRunning(_modernWarfareService) ? string.Format("\n\n" + @"__**Help: Modern Warfare Commands**__

'**{0}mw participants**' to list out the Call of Duty accounts participating in the Modern Warfare services.

'**{0}mw add participant [user]**' to add an account to the list of Call of Duty accounts participating in the Modern Warfare services.
The bot will then ask you to enter the account name, tag, and platform.

'**{0}mw rm participant [user]**' to remove an account from the list of Call of Duty accounts participating in the Modern Warfare services.

'**{0}mw weekly kills**' to display the weekly total game kills of all participating players from highest to lowest.
The bot will then assign the <@&{1}> role to the player in first place.

'**{0}mw lifetime kills**' to display the lifetime total game kills of all participating players from highest to lowest.

'**{0}mw wz wins**' to display the total Warzone wins of all participating players from highest to lowest.
The bot will then assign the <@&{2}> role to the player in first place.", Program.configurationSettingsModel.Prefix, Program.configurationSettingsModel.ModernWarfareKillsRoleID, Program.configurationSettingsModel.ModernWarfareWarzoneWinsRoleID) : null;
        }

        private string HelpBlackOpsColdWarCommands()
        {
            return DisableIfServiceNotRunning(_blackOpsColdWarService) ? string.Format("\n\n" + @"__**Help: Black Ops Cold War Commands**__

'**{0}bocw participants**' to list out the Call of Duty accounts participating in the Black Ops Cold War services.

'**{0}bocw add participant [user]**' to add an account to the list of Call of Duty accounts participating in the Black Ops Cold War services.
The bot will then ask you to enter the account name, tag, and platform.

'**{0}bocw rm participant [user]**' to remove an account from the list of Call of Duty accounts participating in the Black Ops Cold War services.

'**{0}bocw weekly kills**' to display the weekly total game kills of all participating players from highest to lowest.
The bot will then assign the <@&{1}> role to the player in first place.

'**{0}bocw lifetime kills**' to display the lifetime total game kills of all participating players from highest to lowest.", Program.configurationSettingsModel.Prefix, Program.configurationSettingsModel.BlackOpsColdWarKillsRoleID) : null;
        }
    }
}
