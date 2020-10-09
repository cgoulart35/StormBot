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
        private readonly IServiceProvider _services;

        public Soundpad _soundpad;

        public SoundpadCommands(IServiceProvider services)
        {
            _soundpadService = services.GetRequiredService<SoundpadService>();
            _services = services;

            _soundpad = _soundpadService._soundpad;
        }

        [Command("help")]
        public async Task TestCommand()
        {
            string output =
@"Type '~help' to display information on all commands.

Type '~sounds' to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.
                
Type '~sounds [category name]' to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.";

            await ReplyAsync(output);
        }

        [Command("sounds", RunMode = RunMode.Async)]
        public async Task SoundsCommand(params string[] args)
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                string categoryName = GetCategoryArg(args);

                List<Category> sounds = await LoadSounds();
                List<int> soundIndexes = await DisplaySounds(sounds, categoryName);

                if (soundIndexes.Count == 0 && categoryName != null)
                    await ReplyAsync("The category name '" + categoryName + "' does not exist.");
                else
                    await PlayUserSelectedSound(soundIndexes);
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        private string GetCategoryArg(string[] args)
        {
            return args.Length != 0 ? string.Join(" ", args) : null;
        }

        private async Task<List<Category>> LoadSounds()
        {
            CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
            return categoryListResponse.Value.Categories;
        }

        private async Task<List<int>> DisplaySounds(List<Category> categoryList, string categoryName)
        {
            string output = "";

            List<int> soundIndexes = new List<int>();

            int displayedSoundNumber = 1;
            foreach (Category category in categoryList)
            {
                if (category.Name != "All sounds")
                {
                    if (categoryName != null && category.Name != categoryName)
                        continue;

                    output += "**" + category.Name + "**\n";

                    List<Sound> soundList = category.Sounds;

                    foreach (Sound sound in soundList)
                    {
                        output += "     " + displayedSoundNumber + ".) " + sound.Title + "\n";
                        displayedSoundNumber++;
                        soundIndexes.Add(sound.Index);
                    }
                }
            }

            if (output != "")
                await ReplyAsync(output);

            return soundIndexes;
        }

        private async Task PlayUserSelectedSound(List<int> soundIndexes)
        {
            await ReplyAsync("What sound would you like to play? Please answer with a number or 'cancel'.");
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 0, 20));

            // if user responds in time
            if (userSelectResponse != null)
            {
                // if response is not a number
                if (!(int.TryParse(userSelectResponse.Content, out int userSelectNumber)))
                {
                    // if cancel then don't play sound
                    if (userSelectResponse.Content == "cancel" || userSelectResponse.Content == "CANCEL" || userSelectResponse.Content == "Cancel")
                    {
                        await ReplyAsync("Request cancelled.");
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
                    if (userSelectNumber >= 1 && userSelectNumber <= soundIndexes.Count)
                    {
                        await ReplyAsync($"You entered: {userSelectNumber}");
                        await _soundpad.PlaySound(soundIndexes[userSelectNumber - 1]);
                    }
                    // if not valid number, request another response
                    else
                    {
                        await ReplyAsync("Your response was invalid. Please answer a number shown on the list.");
                        await PlayUserSelectedSound(soundIndexes);
                    }
                }
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
            }
        }
    }
}
