using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using cg_bot.Services;
using Discord.Addons.Interactive;
using Discord.Commands;
using MediaToolkit.Model;
using VideoLibrary;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;
using SoundpadConnector.Response;
using SoundpadConnector.XML;
using MediaToolkit;

namespace cg_bot.Modules
{
    public class SoundpadCommands : InteractiveBase<SocketCommandContext>
    {
        private readonly SoundpadService _soundpadService;

        public Soundpad _soundpad;

        private string _categoryFoldersLocation;

        public SoundpadCommands(IServiceProvider services)
        {
            _soundpadService = services.GetRequiredService<SoundpadService>();

            _soundpad = _soundpadService._soundpad;

            _categoryFoldersLocation = Program.CategoryFoldersLocation;
        }



		#region COMMAND FUNCTIONS
		[Command("add", RunMode = RunMode.Async)]
        public async Task AddCommand(params string[] args)
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                // if arguments are valid
                if (args.Length >= 2)
                {
                    string videoURL = args[0];

                    string[] soundNameArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, soundNameArgs, 0, args.Length - 1);
                    string soundName = GetSingleArg(soundNameArgs);

                    // display all category options to user
                    List<int> categoryIndexes = await LoadSounds(true, null, true);

                    // ask user what category to add to
                    int categoryIndex = await AskUserForCategory(categoryIndexes);

                    if (categoryIndex != -1)
                    {
                        CategoryResponse categoryResponse = await _soundpad.GetCategory(categoryIndex + 1);
                        string categoryName = categoryResponse.Value.Name;

                        string source = _categoryFoldersLocation + categoryName + @"\";

                        // try create an MP3 file of the YouTube video with the specified name and location
                        try
                        {
                            SaveMP3(source, videoURL, soundName);

                            // add downloaded sound to soundpad
                            List<int> soundIndexes = await LoadSounds(false, null, false);
                            int newSoundIndex = soundIndexes[soundIndexes.Count - 1] + 1;
                            await _soundpad.AddSound(source + $"{soundName}.mp3", newSoundIndex, categoryIndex + 1);

                            // print out category that the sound was added to
                            await ReplyAsync("Sound added: " + soundName);
                            await LoadSounds(true, categoryName, false);
                        }
                        // video does not exist
                        catch (ArgumentException)
                        {
                            await ReplyAsync("Invalid YouTube video URL.");
                        }
                    }
                }
                else
                {
                    await ReplyAsync("Please enter all required arguments: [YouTube video URL] [sound name]");
                }
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        [Command("categories")]
        public async Task CategoriesCommand()
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                await LoadSounds(true, null, true);
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        [Command("help")]
        public async Task HelpCommand()
        {
            string output = string.Format(
@"Type '{0}add [YouTube video URL] [sound name]' to add a YouTube to MP3 sound to the soundboard in the specified category.
The bot will then ask you to select a category to add the sound to.

Type '{1}categories' to display all categories.

Type '{2}help' to display information on all commands.

Type '{3}pause' to pause/resume the sound currently playing.

Type '{4}play [sound number]' to play the sound with the corresponding number.

Type '{5}sounds' to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.
                
Type '{6}sounds [category name]' to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.

Type '{7}stop' to stop the sound currently playing.", Program.Prefix, Program.Prefix, Program.Prefix, Program.Prefix, Program.Prefix, Program.Prefix, Program.Prefix, Program.Prefix);

            await ReplyAsync(output);
        }

        [Command("pause")]
        public async Task PauseCommand()
        {
            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
            {
                await _soundpad.TogglePause();
            }
            else
                await ReplyAsync("The soundboard is not currently connected.");
        }

        [Command("play", RunMode = RunMode.Async)]
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
		#endregion



		#region COMMAND HELPER FUNCTIONS
		private void SaveMP3(string source, string videoURL, string soundName)
		{
			var youtube = YouTube.Default;
			var video = youtube.GetVideo(videoURL);
			File.WriteAllBytes(source + video.FullName, video.GetBytes());

			var inputFile = new MediaFile { Filename = source + video.FullName };
			var outputFile = new MediaFile { Filename = source + $"{soundName}.mp3" };

			using (var engine = new Engine())
			{
				engine.GetMetadata(inputFile);
				engine.Convert(inputFile, outputFile);
			}

			// after creating the MP3, delete the created MP4 video
			File.Delete(Path.Combine(source, video.FullName));
		}

		private string GetSingleArg(string[] args)
        {
            return args.Length != 0 ? string.Join(" ", args) : null;
        }

        private async Task<List<int>> LoadSounds(bool displayOutput, string categoryName = null, bool categoriesMode = false)
        {
            CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
            List<Category> categoryList = categoryListResponse.Value.Categories;

            List<string> output = new List<string>();
            output.Add("");

            List<int> indexes = new List<int>();

            int displayedSoundNumber = 1;
            int displayedCategoryNumber = 1;
            foreach (Category category in categoryList)
            {
                if (category.Name != "All sounds")
                {
                    if (categoryName == null || category.Name == categoryName)
                        output = ValidateOutputLimit(output, "\n**" + displayedCategoryNumber + ".) " + category.Name + "**");

                    if (!categoriesMode)
                    {
                        List<Sound> soundList = category.Sounds;

                        foreach (Sound sound in soundList)
                        {
                            if (categoryName == null || category.Name == categoryName)
                                output = ValidateOutputLimit(output, "\n" + "     " + displayedSoundNumber + ".) " + sound.Title);
                            displayedSoundNumber++;
                            indexes.Add(sound.Index);
                        }
                    }
                    else
                    {
                        indexes.Add(category.Index);
                    }

                    displayedCategoryNumber++;
                }
            }

            if (displayOutput && output[0] != "")
            {
                foreach (string chunk in output)
                {
                    await ReplyAsync(chunk);
                }
            }

            return indexes;
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
                await PlaySoundNumber(soundIndexes, userSelectResponse.Content, true);
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
            }
        }

        private async Task PlaySoundNumber(List<int> soundIndexes, string requestedNumber, bool waitingForAnswer = false)
        {
            // if response is not a number
            if (!(int.TryParse(requestedNumber, out int validatedNumber)))
            {
                // if cancel then don't play sound
                if (requestedNumber == "cancel" || requestedNumber == "CANCEL" || requestedNumber == "Cancel")
                {
                    await ReplyAsync("Request cancelled.");
                }
                // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                else if (requestedNumber != null && requestedNumber.StartsWith(Program.Prefix) && waitingForAnswer)
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
        
        private async Task<int> AskUserForCategory(List<int> categoryIndexes)
        {
            await ReplyAsync("What category would you like to add the sound to? Please answer with a number or 'cancel'.");
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 0, 20));

            // if user responds in time
            if (userSelectResponse != null)
            {
                string requestedNumber = userSelectResponse.Content;

                // if response is not a number
                if (!(int.TryParse(requestedNumber, out int validatedNumber)))
                {
                    // if cancel then don't return category index
                    if (requestedNumber == "cancel" || requestedNumber == "CANCEL" || requestedNumber == "Cancel")
                    {
                        await ReplyAsync("Request cancelled.");
                        return -1;
                    }
                    // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                    else if (requestedNumber != null && requestedNumber.StartsWith(Program.Prefix))
                    {
                        return -1;
                    }
                    // if not cancel, request another response
                    else
                    {
                        await ReplyAsync("Your response was invalid. Please answer with a number.");
                        return await AskUserForCategory(categoryIndexes);
                    }
                }
                // if response is a number
                else
                {
                    // if number is valid option on list of categories
                    if (validatedNumber >= 1 && validatedNumber <= categoryIndexes.Count)
                    {
                        await ReplyAsync($"You entered: {validatedNumber}");
                        return validatedNumber;
                    }
                    // if not valid number, request another response
                    else
                    {
                        await ReplyAsync("Your response was invalid. Please answer a number shown on the list.");
                        return await AskUserForCategory(categoryIndexes);
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
        #endregion
    }
}
