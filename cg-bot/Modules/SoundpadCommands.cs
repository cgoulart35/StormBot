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
                string categoryName = null;

                if (args.Length != 0)
                    categoryName = string.Join(" ", args);

                List<int> soundIndexes;
                soundIndexes = await LoadSounds(categoryName);

                if (soundIndexes.Count == 0 && categoryName != null)
                    await ReplyAsync("The category name '" + categoryName + "' does not exist.");
                else
                    await PlayUserSelectedSound(soundIndexes);
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        private async Task<List<int>> LoadSounds(string categoryName = null)
        {
            CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
            List<Category> categoryList = categoryListResponse.Value.Categories;

            return await DisplaySounds(categoryList, categoryName);
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
            await ReplyAsync("What sound would you like to play? Please answer with a number.");
            var userSelectResponse = await NextMessageAsync();//( new TimeSpan(0, 0, 10));

            if (userSelectResponse != null)
            {
                await ReplyAsync($"You entered: {userSelectResponse.Content}");

                int userSelectNumber = int.Parse(userSelectResponse.Content);

                await _soundpad.PlaySound(soundIndexes[userSelectNumber - 1]);
            }
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
            }
        }
    }
}
