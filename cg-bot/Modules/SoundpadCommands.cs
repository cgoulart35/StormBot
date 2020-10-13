using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cg_bot.Services;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;
using SoundpadConnector.Response;
using SoundpadConnector.XML;

namespace cg_bot.Modules
{
    public class SoundpadCommands : InteractiveBase<SocketCommandContext>
    {
        private readonly SoundpadService _soundpadService;

        public Soundpad _soundpad;

        public SoundpadCommands(IServiceProvider services)
        {
            _soundpadService = services.GetRequiredService<SoundpadService>();

            _soundpad = _soundpadService._soundpad;
        }

        [Command("help")]
        public async Task HelpCommand()
        {
            string output =
@"Type '~help' to display information on all commands.

Type '~play [sound number]' to play the sound with the corresponding number.

Type '~sounds' to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.
                
Type '~sounds [category name]' to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.

Type '~stop' to stop the sound currently playing.";

            await ReplyAsync(output);
        }

        [Command("play")]
        public async Task PlayCommand(params string[] args)
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                string soundNumber = GetSingleArg(args);

                List<int> soundIndexes = await LoadSounds(false);

                await PlaySoundNumber(soundIndexes, soundNumber);
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        [Command("stop")]
        public async Task StopCommand()
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
				await _soundpad.StopSound();
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        [Command("sounds", RunMode = RunMode.Async)]
        public async Task SoundsCommand(params string[] args)
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                string categoryName = GetSingleArg(args);

                List<int> soundIndexes = await LoadSounds(true, categoryName);

                if (soundIndexes.Count == 0 && categoryName != null)
                    await ReplyAsync("The category name '" + categoryName + "' does not exist.");
                else
                    await PlayUserSelectedSound(soundIndexes);
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        private string GetSingleArg(string[] args)
        {
            return args.Length != 0 ? string.Join(" ", args) : null;
        }

        private async Task<List<int>> LoadSounds(bool displaySounds, string categoryName = null)
        {
            CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
            List<Category> categoryList = categoryListResponse.Value.Categories;

            List<string> output = new List<string>();
            output.Add("");

            List<int> soundIndexes = new List<int>();

            int displayedSoundNumber = 1;
            foreach (Category category in categoryList)
            {
                if (category.Name != "All sounds")
                {
                    if (categoryName != null && category.Name != categoryName)
                        continue;

                    output = ValidateOutputLimit(output, "\n**" + category.Name + "**");

                    List<Sound> soundList = category.Sounds;

                    foreach (Sound sound in soundList)
                    {
                        output = ValidateOutputLimit(output, "\n" + "     " + displayedSoundNumber + ".) " + sound.Title);
                        displayedSoundNumber++;
                        soundIndexes.Add(sound.Index);
                    }
                }
            }

            if (displaySounds && output[0] != "")
            {
                foreach (string chunk in output)
                {
                    await ReplyAsync(chunk);
                }
            }

            return soundIndexes;
        }

        private List<string> ValidateOutputLimit(List<string> output, string messageToAdd)
        {
            string temp = output[output.Count - 1] + messageToAdd;
            if (temp.Length <= 2000)
            {
                output[output.Count - 1] += messageToAdd;
                return output;
            }
            else
            {
                output.Add("\n" + "...");
                return ValidateOutputLimit(output, messageToAdd);
            }
        }

        private async Task PlayUserSelectedSound(List<int> soundIndexes)
        {
            await ReplyAsync("What sound would you like to play? Please answer with a number or 'cancel'.");
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 1, 0));

            // if user responds in time
            if (userSelectResponse != null)
            {
                await PlaySoundNumber(soundIndexes, userSelectResponse.Content);
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
            }
        }

        private async Task PlaySoundNumber(List<int> soundIndexes, string requestedNumber)
        {
            // if response is not a number
            if (!(int.TryParse(requestedNumber, out int validatedNumber)))
            {
                // if cancel then don't play sound
                if (requestedNumber == "cancel" || requestedNumber == "CANCEL" || requestedNumber == "Cancel")
                {
                    await ReplyAsync("Request cancelled.");
                }
                // if same user starts another command, end this one but don't display request cancelled
                else if (requestedNumber.StartsWith("~"))
                {
                }
                // if not cancel, request another response
                else
                {
                    await ReplyAsync("Your response was invalid. Please answer with a number.");
                    await PlayUserSelectedSound(soundIndexes);
                }
            }
            // if response is a number
            else
            {
                // if number is valid option on list of sounds
                if (validatedNumber >= 1 && validatedNumber <= soundIndexes.Count)
                {
                    await ReplyAsync($"You entered: {validatedNumber}");
                    await _soundpad.PlaySound(soundIndexes[validatedNumber - 1]);
                }
                // if not valid number, request another response
                else
                {
                    await ReplyAsync("Your response was invalid. Please answer a number shown on the list.");
                    await PlayUserSelectedSound(soundIndexes);
                }
            }
        }
    }
}
