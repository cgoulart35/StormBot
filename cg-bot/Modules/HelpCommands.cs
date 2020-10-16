using System.Threading.Tasks;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class HelpCommands : BaseCommandModule
    {
        [Command("help", RunMode = RunMode.Async)]
        public async Task HelpCommand(params string[] args)
        {
            string subject = GetSingleArg(args);
            string output = "";

            if (subject == null)
            {
                output = string.Format(@"{0}

{1}

{2}", HelpHelpCommands(), HelpSoundboardCommands(), HelpModernWarfareCommands());
            }
            else if (subject.ToLower() == "help")
            {
                output = HelpHelpCommands();
            }
            else if (subject.ToLower() == "sb" || subject.ToLower() == "sp" || subject.ToLower() == "soundboard" || subject.ToLower() == "soundpad")
            {
                output = HelpSoundboardCommands();
            }
            else if (subject.ToLower() == "mw" || subject.ToLower() == "modern warfare" || subject.ToLower() == "modernwarfare")
            {
                output = HelpModernWarfareCommands();
            }
            else
            {
                output = "The subject name '" + subject + "' does not exist.";
            }

            await ReplyAsync(output);
        }

        private string HelpHelpCommands()
        {
            return string.Format(@"__**Help: Help Commands**__

'**{0}help**' to display information on all commands.

'**{0}help** [subject]' to display information on all commands for a specific subject.", Program.Prefix);
        }

        private string HelpSoundboardCommands()
        {
            return string.Format(@"__**Help: Soundboard Commands**__

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

'**{0}stop**' to stop the sound currently playing.", Program.Prefix);
        }

        private string HelpModernWarfareCommands()
        {
            return string.Format(@"__**Help: Modern Warfare Commands**__

'**{0}mwtest**' to test the mw module", Program.Prefix);
        }
    }
}
