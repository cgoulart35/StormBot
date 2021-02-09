using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SoundpadConnector;
using SoundpadConnector.Response;
using SoundpadConnector.XML;
using VideoLibrary;
using StormBot.Services;
using StormBot.Database;

namespace StormBot.Modules
{
    public class SoundpadCommands : BaseCommand
    {
        private SoundpadService _service;

        private StormBotContext _db;

        private Soundpad _soundpad;

        private readonly string _categoryFoldersLocation = Program.configurationSettingsModel.CategoryFoldersLocation;

        private static Dictionary<string, int> _adminApprovalRequests = new Dictionary<string, int>();
        
        public SoundpadCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<SoundpadService>();
            _db = services.GetRequiredService<StormBotContext>();
            _soundpad = _service._soundpad;
        }

        #region COMMAND FUNCTIONS
        [Command("add", RunMode = RunMode.Async)]
        public async Task AddCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "add"))
                    {
                        await Context.Channel.TriggerTypingAsync();

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
                                Tuple<List<int>, bool> loadedSounds = await LoadSounds(true, null, true);
                                List<int> categoryIndexes = loadedSounds.Item1;

                                var user = Context.User as SocketGuildUser;

                                // ask user what category to add to
                                int categoryIndex = await AskUserForCategory(categoryIndexes, user.Id);

                                // get instance of the YouTube video
                                YouTubeVideo video = await GetYouTubeVideo(videoURL);

                                // unless cancelled, continue adding the sound
                                if (categoryIndex != -1)
                                {
                                    // unless video doesn't exist, get admin approval 
                                    if (video != null)
                                    {
                                        // add sound if admin or if an admin approves
                                        if (user.GuildPermissions.Administrator || await AskAdministratorForApproval(user))
                                        {
                                            await AddNewSound(categoryIndex, video, soundName);
                                        }
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
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("approve", RunMode = RunMode.Async)]
        public async Task ApproveCommand(SocketGuildUser user)
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "approve"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                            {
                                string username = user.Username;
                                if (_adminApprovalRequests.ContainsKey(username))
                                {
                                    await ReplyAsync($"<@!{user.Id}>'s request has been approved.");
                                    _adminApprovalRequests[username] = 1;
                                }
                                else
                                {
                                    await ReplyAsync($"<@!{user.Id}> is not awaiting an approval.");
                                }
                            }
                            else
                                await ReplyAsync("The soundboard is not currently connected.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("categories", RunMode = RunMode.Async)]
        public async Task CategoriesCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "categories"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            await LoadSounds(true, null, true);
                        }
                        else
                            await ReplyAsync("The soundboard is not currently connected.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("delete", RunMode = RunMode.Async)]
        public async Task DeleteCommand(params string[] args) 
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "delete"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                            {
                                if (args.Length == 1)
                                {
                                    string soundNumber = GetSingleArg(args);

                                    Tuple<List<int>, bool> loadedSounds = await LoadSounds(false);
                                    List<int> soundIndexes = loadedSounds.Item1;

                                    await PlayOrDeleteSoundNumber(soundIndexes, soundNumber, false, true);
                                }
                                else
                                    await ReplyAsync("Please enter all required arguments: [sound number]");
                            }
                            else
                                await ReplyAsync("The soundboard is not currently connected.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("deny", RunMode = RunMode.Async)]
        public async Task DenyCommand(SocketGuildUser user)
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "deny"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                        {
                            await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                        }
                        else
                        {
                            if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                            {
                                string username = user.Username;
                                if (_adminApprovalRequests.ContainsKey(username))
                                {
                                    await ReplyAsync($"<@!{user.Id}>'s request has been denied.");
                                    _adminApprovalRequests[username] = 0;
                                }
                                else
                                {
                                    await ReplyAsync($"<@!{user.Id}> is not awaiting an approval.");
                                }
                            }
                            else
                                await ReplyAsync("The soundboard is not currently connected.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("pause", RunMode = RunMode.Async)]
        public async Task PauseCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "pause"))
                    {
                        if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            await _soundpad.TogglePause();
                        }
                        else
                            await ReplyAsync("The soundboard is not currently connected.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task PlayCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "play"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            if (args.Length == 1)
                            {
                                string soundNumber = GetSingleArg(args);

                                Tuple<List<int>, bool> loadedSounds = await LoadSounds(false);
                                List<int> soundIndexes = loadedSounds.Item1;

                                await PlayOrDeleteSoundNumber(soundIndexes, soundNumber);
                            }
                            else
                                await ReplyAsync("Please enter all required arguments: [sound number]");
                        }
                        else
                            await ReplyAsync("The soundboard is not currently connected.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("sounds", RunMode = RunMode.Async)]
        public async Task SoundsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "sounds"))
                    {
                        await Context.Channel.TriggerTypingAsync();

                        if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            string categoryName = GetSingleArg(args);

                            Tuple<List<int>, bool> loadedSounds = await LoadSounds(true, categoryName);
                            List<int> soundIndexes = loadedSounds.Item1;
                            bool categoryExists = loadedSounds.Item2;

                            if (categoryName != null && !categoryExists)
                                await ReplyAsync("The category name '" + categoryName + "' does not exist.");
                            else
                                await PlayOrDeleteUserSelectedSound(soundIndexes);
                        }
                        else
                            await ReplyAsync("The soundboard is not currently connected.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await _service.GetServerAllowServerPermissionSoundpadCommands(Context) && await _service.GetServerToggleSoundpadCommands(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "stop"))
                    {
                        if (_soundpad.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            await _soundpad.StopSound();
                        }
                        else
                            await ReplyAsync("The soundboard is not currently connected.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }
        #endregion

        #region COMMAND HELPER FUNCTIONS     
        private async Task<Tuple<List<int>, bool>> LoadSounds(bool displayOutput, string categoryName = null, bool categoriesMode = false)
        {
            CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
            List<Category> categoryList = categoryListResponse.Value.Categories;

            // keep track of whether of not it is a valid name; only important when category name provided
            bool categoryExists = false;

            List<string> output = new List<string>();
            output.Add("");

            List<int> indexes = new List<int>();

            int displayedSoundNumber = 1;
            int displayedCategoryNumber = 1;
            foreach (Category category in categoryList)
            {
                if (category.Name != "All sounds")
                {
                    if (category.Name == categoryName)
                        categoryExists = true;

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

            return Tuple.Create(indexes, categoryExists);
        }

        private async Task PlayOrDeleteUserSelectedSound(List<int> soundIndexes, bool deleteMode = false)
        {
            string verb = "play";
            if (deleteMode)
                verb = "delete";

            await ReplyAsync($"What sound would you like to {verb}? Please answer with a number or 'cancel'.");
            var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 1, 0));

            // if user responds in time
            if (userSelectResponse != null)
            {
                await PlayOrDeleteSoundNumber(soundIndexes, userSelectResponse.Content, true, deleteMode);
            }
            // if user doesn't respond in time
            else
            {
                await ReplyAsync("You did not reply before the timeout.");
            }
        }

        private async Task PlayOrDeleteSoundNumber(List<int> soundIndexes, string requestedNumber, bool waitingForAnswer = false, bool deleteMode = false)
        {
            ulong userID = Context.User.Id;

            // if response is not a number
            if (!(int.TryParse(requestedNumber, out int validatedNumber)))
            {
                // if nothing, don't do anything
                if (requestedNumber == null)
                {
                    
                }
                // if response is cancel and waiting response, don't play sound and display cancelled message
                else if (requestedNumber.ToLower() == "cancel" && waitingForAnswer)
                {
                    await ReplyAsync("Request cancelled.");
                }
                // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                else if (requestedNumber.StartsWith(await GetServerPrefix(_db)) && waitingForAnswer)
                {
                }
                // if not cancel, request another response
                else
                {
                    await ReplyAsync($"<@!{userID}>, your response was invalid. Please answer with a number.");
                    await PlayOrDeleteUserSelectedSound(soundIndexes, deleteMode);
                }
            }
            // if response is a number
            else
            {
                // if number is valid option on list of sounds
                if (validatedNumber >= 1 && validatedNumber <= soundIndexes.Count)
                {
                    await ReplyAsync($"<@!{userID}> entered: {validatedNumber}");

                    int soundIndex = soundIndexes[validatedNumber - 1];

                    if (deleteMode)
                    {
                        string soundName = "";

                        // get the sound's name to delete
                        CategoryListResponse categoryListResponse = await _soundpad.GetCategories(true);
                        List<Category> categoryList = categoryListResponse.Value.Categories;
                        foreach (Category category in categoryList)
                        {
                            List<Sound> soundList = category.Sounds;
                            foreach (Sound sound in soundList)
                            {
                                if (sound.Index == soundIndex)
                                {
                                    soundName = sound.Title;
                                    break;
                                }
                            }
                        }

                        // select desired sound
                        await _soundpad.SelectIndex(soundIndex);

                        // wait for sound to be selected before removing selected sound
                        await Task.Delay(2000);

                        // delete selected sound 
                        await _soundpad.RemoveSelectedEntries(true);

                        await ReplyAsync("Sound deleted: " + soundName);
                    }
                    else
                        await _soundpad.PlaySound(soundIndex);
                }
                // if not valid number, request another response
                else
                {
                    await ReplyAsync($"<@!{userID}>, your response was invalid. Please answer a number shown on the list.");
                    await PlayOrDeleteUserSelectedSound(soundIndexes, deleteMode);
                }
            }
        }

        private async Task<int> AskUserForCategory(List<int> categoryIndexes, ulong userID)
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
                    if (requestedNumber.ToLower() == "cancel")
                    {
                        await ReplyAsync("Request cancelled.");
                        return -1;
                    }
                    // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                    else if (requestedNumber != null && requestedNumber.StartsWith(await GetServerPrefix(_db)))
                    {
                        return -1;
                    }
                    // if not cancel, request another response
                    else
                    {
                        await ReplyAsync($"<@!{userID}>, your response was invalid. Please answer with a number.");
                        return await AskUserForCategory(categoryIndexes, userID);
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
                        await ReplyAsync($"<@!{userID}>, your response was invalid. Please answer a number shown on the list.");
                        return await AskUserForCategory(categoryIndexes, userID);
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

        private async Task<YouTubeVideo> GetYouTubeVideo(string videoURL)
        {
            // try create an instance of the YouTube video with the specified url
            try
            {
                YouTube youtube = YouTube.Default;
                YouTubeVideo video = youtube.GetVideo(videoURL);
                return video;
            }
            // video does not exist
            catch (ArgumentException)
            {
                await ReplyAsync("Invalid YouTube video URL.");
                return null;
            }
        }

        private async Task<bool> AskAdministratorForApproval(SocketGuildUser user)
        {
            // if user is already awaiting an approval on a request, block an additional request
            if (_adminApprovalRequests.ContainsKey(user.Username))
            {
                await ReplyAsync($"<@!{user.Id}>, you are already waiting on a request to be approved.");
                return false;
            }

            // add request with requesting user's name and initial approval value of 0
            _adminApprovalRequests.Add(user.Username, -1);

            await ReplyAsync($"<@!{user.Id}>, your request to add to the soundboard is awaiting approval.");

            // create timer to keep track of elapsed time
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // wait for an approval value from an admin or timeout
            while (_adminApprovalRequests[user.Username] == -1 && timer.Elapsed.TotalMinutes < 5) { }

            timer.Stop();

            // request denied
            if (_adminApprovalRequests[user.Username] == 0)
            {
                _adminApprovalRequests.Remove(user.Username);
                return false;
            }
            // request approved
            else if (_adminApprovalRequests[user.Username] == 1)
            {
                _adminApprovalRequests.Remove(user.Username);
                return true;
            }
            // request timed out
            else
            {
                await ReplyAsync($"<@!{user.Id}>, your request was not approved before the timeout.");
                _adminApprovalRequests.Remove(user.Username);
                return false;
            }
        }

        private async Task AddNewSound(int categoryIndex, YouTubeVideo video, string soundName)
        {
            CategoryResponse categoryResponse = await _soundpad.GetCategory(categoryIndex + 1);
            string categoryName = categoryResponse.Value.Name;

            string source = _categoryFoldersLocation + categoryName + @"\";

            // download video and convert to MP3
            bool isSaved = await SaveMP3(source, video, soundName);

            if (isSaved)
            {
                // add downloaded sound to soundpad
                Tuple<List<int>, bool> loadedSounds = await LoadSounds(false, null, false);
                List<int> soundIndexes = loadedSounds.Item1;
                int newSoundIndex = soundIndexes[soundIndexes.Count - 1] + 1;
                await _soundpad.AddSound(source + $"{soundName}.mp3", newSoundIndex, categoryIndex + 1);

                // wait for sound to be added before printing category
                await Task.Delay(2000);

                // print out category that the sound was added to
                await LoadSounds(true, categoryName, false);
                await ReplyAsync("Sound added: " + soundName);
            }
        }

        private async Task<bool> SaveMP3(string source, YouTubeVideo video, string soundName)
        {
            string fileName = source + soundName + ".mp4";
            File.WriteAllBytes(fileName, video.GetBytes());

            try
            {
                File.Move(fileName, Path.ChangeExtension(fileName, ".mp3"));
            }
            catch (IOException)
            {
                await ReplyAsync($"A sound with the name '{soundName}' already exists in this category.");
                File.Delete(Path.Combine(source, fileName));
                return false;
            }

            return true;

            /* DEPRECATED
             * 
             * MediaToolKit not supported on .NET CORE

            File.WriteAllBytes(fileName, video.GetBytes());

            var inputFile = new MediaFile { Filename = source + video.FullName };
            var outputFile = new MediaFile { Filename = source + $"{soundName}.mp3" };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            // after creating the MP3, delete the created MP4 video
            File.Delete(Path.Combine(source, video.FullName));
            */
        }
        #endregion
    }
}
