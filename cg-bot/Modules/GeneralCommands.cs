using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using cg_bot.Services;
using cg_bot.Database;
using cg_bot.Database.Entities;

namespace cg_bot.Modules
{
	public class GeneralCommands : BaseCommand
    {
        private SoundpadService _soundpadService;
        private CallOfDutyService _callOfDutyService;
        private CgBotContext _db;

        public GeneralCommands(IServiceProvider services)
        {
            _soundpadService = services.GetRequiredService<SoundpadService>();
            _callOfDutyService = services.GetRequiredService<CallOfDutyService>();
            _db = services.GetRequiredService<CgBotContext>();
        }

        /*
         * TODO: add mini game commands to their own service
         * 
        [Command("play battleship with", RunMode = RunMode.Async)]
        public async Task BattleshipCommand(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();

            try
            {
                string input = GetSingleArg(args);

                ulong discordID = GetDiscordUserID(input);

                // make sure initiating discord user and provided discord user are both in different colored teams
                // make sure only players allowed to respond, 5 minute wait or game aborted; different spots that don't over lap; smaller ships and grid?
                // add to a currently playing queue/object/list; grid with marked hits, misses, and ships; check every play for H, M, S, and E markers on players' grids
                // message provided user in their teams's channel that initiator is placing ships
                // prompt initiator in initator's channel to place ships
                // message initiator in their team' channel that provided user is placing ships
                // prompt provided user in their team's channel to place ships
                // initiator's first move; show previous hits and misses
                // message other user to wait
                // provided user's first move; show previous hits and misses
                // message other user to wait
                // once all ships sunk, player still standing wins

            }
            catch
            {
                await ReplyAsync("Please provide a valid Discord user.");
            }
        }
        */

        #region COMMAND FUNCTIONS
        // admin role command
        [Command("config all", RunMode = RunMode.Async)]
        public async Task ConfigAllCommand()
        {
            await Context.Channel.TriggerTypingAsync();

            if (!Context.IsPrivate)
            {
                if (!(((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db))) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                {
                    await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                }
                else
                {
                    ServersEntity serverData = await _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == Context.Guild.Id)
                        .SingleAsync();

                    string message = string.Format(@"__**Current Configurations:**__

**Prefix:** {0}
**Black Ops Cold War Tracking Feature:** {1}
**Modern Warfare Tracking Feature:** {2}
**Warzone Tracking Feature:** {3}
**Soundboard Feature:** {4}
**Call of Duty notification channel:** <#{5}>
**Soundboard notification channel:** <#{6}>
**Admin role:** <@&{7}>
**Black Ops Cold War kills role:** <@&{8}>
**Modern Warfare kills role:** <@&{9}>
**Warzone wins role:** <@&{10}>
**Warzone kills role:** <@&{11}>", serverData.PrefixUsed, serverData.ToggleBlackOpsColdWarTracking ? "On" : "Off", serverData.ToggleModernWarfareTracking ? "On" : "Off", serverData.ToggleWarzoneTracking ? "On" : "Off", serverData.ToggleSoundpadCommands ? "On" : "Off", serverData.CallOfDutyNotificationChannelID, serverData.SoundboardNotificationChannelID, serverData.AdminRoleID, serverData.BlackOpsColdWarKillsRoleID, serverData.ModernWarfareKillsRoleID, serverData.WarzoneWinsRoleID, serverData.WarzoneKillsRoleID);

                    await ReplyAsync(message);
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config prefix", RunMode = RunMode.Async)]
        public async Task ConfigPrefixCommand(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();

            if (!Context.IsPrivate)
            {
                if (!(((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db))) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                {
                    await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                }
                else
                {
                    if (args.Length == 1)
                    {
                        string prefix = GetSingleArg(args);

                        string currentPrefix = await GetServerPrefix(_db);
                        if (prefix != currentPrefix)
                        {
                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            serverData.PrefixUsed = prefix;
                            await _db.SaveChangesAsync();

                            await ReplyAsync("The server prefix was set to: " + await GetServerPrefix(_db));
                        }
                        else
                            await ReplyAsync("The server is already using this prefix.");
                    }
                    else
                        await ReplyAsync("Please enter all required arguments: [prefix]");
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }
        
        // admin role command
        [Command("config toggle bocw", RunMode = RunMode.Async)]
        public async Task ConfigToggleBocwTrackingCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        ServersEntity serverData = await _db.Servers
                            .AsQueryable()
                            .Where(s => s.ServerID == Context.Guild.Id)
                            .SingleAsync();

                        bool flag = serverData.ToggleBlackOpsColdWarTracking;
                        serverData.ToggleBlackOpsColdWarTracking = !flag;
                        await _db.SaveChangesAsync();

                        if (!flag)
                            await ReplyAsync("Black Ops Cold War tracking was enabled.");
                        else
                            await ReplyAsync("Black Ops Cold War tracking was disabled.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config toggle mw", RunMode = RunMode.Async)]
        public async Task ConfigToggleMwTrackingCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionModernWarfareTracking(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        ServersEntity serverData = await _db.Servers
                            .AsQueryable()
                            .Where(s => s.ServerID == Context.Guild.Id)
                            .SingleAsync();

                        bool flag = serverData.ToggleModernWarfareTracking;
                        serverData.ToggleModernWarfareTracking = !flag;
                        await _db.SaveChangesAsync();

                        if (!flag)
                            await ReplyAsync("Modern Warfare tracking was enabled.");
                        else
                            await ReplyAsync("Modern Warfare tracking was disabled.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config toggle wz", RunMode = RunMode.Async)]
        public async Task ConfigToggleWzTrackingCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionWarzoneTracking(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        ServersEntity serverData = await _db.Servers
                            .AsQueryable()
                            .Where(s => s.ServerID == Context.Guild.Id)
                            .SingleAsync();

                        bool flag = serverData.ToggleWarzoneTracking;
                        serverData.ToggleWarzoneTracking = !flag;
                        await _db.SaveChangesAsync();

                        if (!flag)
                            await ReplyAsync("Warzone tracking was enabled.");
                        else
                            await ReplyAsync("Warzone tracking was disabled.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config toggle sb", RunMode = RunMode.Async)]
        public async Task ConfigToggleSbCommand()
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        ServersEntity serverData = await _db.Servers
                            .AsQueryable()
                            .Where(s => s.ServerID == Context.Guild.Id)
                            .SingleAsync();

                        bool flag = serverData.ToggleSoundpadCommands;
                        serverData.ToggleSoundpadCommands = !flag;
                        await _db.SaveChangesAsync();

                        if (!flag)
                            await ReplyAsync("Soundboard commands were enabled.");
                        else
                            await ReplyAsync("Soundboard commands were disabled.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config channel cod", RunMode = RunMode.Async)]
        public async Task ConfigChannelCodCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if ((await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db)) || (await GetServerAllowServerPermissionModernWarfareTracking(_db) && await GetServerToggleModernWarfareTracking(_db)) || (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db)))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordChannelID = GetDiscordID(input, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.CallOfDutyNotificationChannelID != discordChannelID)
                            {
                                serverData.CallOfDutyNotificationChannelID = discordChannelID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Call of Duty notification channel has been set to: <#{discordChannelID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this channel for Call of Duty notifications.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord channel.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config channel sb", RunMode = RunMode.Async)]
        public async Task ConfigChannelSbCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordChannelID = GetDiscordID(input, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.SoundboardNotificationChannelID != discordChannelID)
                            {
                                serverData.SoundboardNotificationChannelID = discordChannelID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Soundboard notification channel has been set to: <#{discordChannelID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this channel for Soundboard notifications.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord channel.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config role admin", RunMode = RunMode.Async)]
        public async Task ConfigRoleAdminCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.AdminRoleID != discordRoleID)
                            {
                                serverData.AdminRoleID = discordRoleID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The admin role has been set to: <@&{discordRoleID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this role as the admin role.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord role.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config role bocw kills", RunMode = RunMode.Async)]
        public async Task ConfigRoleBocwKillsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.BlackOpsColdWarKillsRoleID != discordRoleID)
                            {
                                serverData.BlackOpsColdWarKillsRoleID = discordRoleID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Black Ops Cold War kills role has been set to: <@&{discordRoleID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this role as the Black Ops Cold War kills role.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord role.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config role mw kills", RunMode = RunMode.Async)]
        public async Task ConfigRoleMwKillsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.ModernWarfareKillsRoleID != discordRoleID)
                            {
                                serverData.ModernWarfareKillsRoleID = discordRoleID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Modern Warfare kills role has been set to: <@&{discordRoleID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this role as the Modern Warfare kills role.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord role.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config role wz wins", RunMode = RunMode.Async)]
        public async Task ConfigRoleWzWinsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.WarzoneWinsRoleID != discordRoleID)
                            {
                                serverData.WarzoneWinsRoleID = discordRoleID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Warzone wins role has been set to: <@&{discordRoleID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this role as the Warzone wins role.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord role.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config role wz kills", RunMode = RunMode.Async)]
        public async Task ConfigRoleWzKillsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            ServersEntity serverData = await _db.Servers
                                .AsQueryable()
                                .Where(s => s.ServerID == Context.Guild.Id)
                                .SingleAsync();

                            if (serverData.WarzoneKillsRoleID != discordRoleID)
                            {
                                serverData.WarzoneKillsRoleID = discordRoleID;
                                await _db.SaveChangesAsync();

                                await ReplyAsync($"The Warzone kills role has been set to: <@&{discordRoleID}>");
                            }
                            else
                                await ReplyAsync("The server is already using this role as the Warzone kills role.");
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord role.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task HelpCommand(params string[] args)
        {
            string subject = GetSingleArg(args);
            List<string> output = new List<string>();
            output.Add("");

            bool notAdmin = true;
            if (!Context.IsPrivate)
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            if (subject == null)
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                output = ValidateOutputLimit(output, await HelpHelpCommands());
                output = ValidateOutputLimit(output, await HelpConfigCommands());
                output = ValidateOutputLimit(output, await HelpSoundboardCommands(notAdmin));
                output = ValidateOutputLimit(output, await HelpBlackOpsColdWarCommands(notAdmin));
                output = ValidateOutputLimit(output, await HelpModernWarfareCommands(notAdmin));
                output = ValidateOutputLimit(output, await HelpWarzoneCommands(notAdmin));
            }
            else if (subject.ToLower() == "help")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();
                output = ValidateOutputLimit(output, await HelpHelpCommands());
            }
            else if (subject.ToLower() == "config" || subject.ToLower() == "configs" || subject.ToLower() == "configuration" || subject.ToLower() == "configurations")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();
                output = ValidateOutputLimit(output, await HelpConfigCommands());
            }
            else if (subject.ToLower() == "sb" || subject.ToLower() == "sp" || subject.ToLower() == "soundboard" || subject.ToLower() == "soundpad")
            {
                output = ValidateOutputLimit(output, await HelpSoundboardCommands(notAdmin));
            }
            else if (subject.ToLower() == "mw" || subject.ToLower() == "modern warfare" || subject.ToLower() == "modernwarfare")
            {
                output = ValidateOutputLimit(output, await HelpModernWarfareCommands(notAdmin));
            }
            else if (subject.ToLower() == "wz" || subject.ToLower() == "warzone")
            {
                output = ValidateOutputLimit(output, await HelpWarzoneCommands(notAdmin));
            }
            else if (subject.ToLower() == "bocw" || subject.ToLower() == "black ops cold war" || subject.ToLower() == "blackopscoldwar" || subject.ToLower() == "cw" || subject.ToLower() == "cold war" || subject.ToLower() == "coldwar")
            {
                output = ValidateOutputLimit(output, await HelpBlackOpsColdWarCommands(notAdmin));
            }
            else
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();
                output = ValidateOutputLimit(output, "The subject name '" + subject + "' does not exist, or is not available.");
            }

            if (output[0] != "")
            {
                foreach (string chunk in output)
                {
                    if (!notAdmin)
                        await ReplyAsync(chunk);
                    else
                        await Context.User.SendMessageAsync(chunk);
                }
            }
        }

        [Command("subjects", RunMode = RunMode.Async)]
        public async Task SubjectsCommand()
        {
            bool notAdmin = true;
            if (!Context.IsPrivate)
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(await GetServerAdminRole(_db)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            if (!notAdmin)
                await Context.Channel.TriggerTypingAsync();

            string output = "__**Subjects:**__\nHelp\nConfig\n";

            if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
            {
                if (DisableIfServiceNotRunning(_soundpadService, "subjects (soundpad subject)"))
                {
                    output += "Soundpad\n";
                }
            }
            if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.BlackOpsColdWarComponent, "subjects (Black Ops Cold War subject)"))
                {
                    output += "Black Ops Cold War\n";
                }
            }
            if (await GetServerAllowServerPermissionModernWarfareTracking(_db) && await GetServerToggleModernWarfareTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.ModernWarfareComponent, "subjects (Modern Warfare subject)"))
                {
                    output += "Modern Warfare\n";
                }
            }
            if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.WarzoneComponent, "subjects (Warzone subject)"))
                {
                    output += "Warzone\n";
                }
            }

            if (!notAdmin)
                await ReplyAsync(output);
            else
                await Context.User.SendMessageAsync(output);
        }
		#endregion

		private async Task<string> HelpHelpCommands()
        {
            return string.Format("\n\n" + @"__**Help: Help Commands**__

'**{0}help**' to display information on all commands.
'**{0}help [subject]**' to display information on all commands for a specific subject.
'**{0}subjects**' to display the existing command subjects.", await GetServerPrefix(_db));
        }

        private async Task<string> HelpConfigCommands()
        {
            return string.Format("\n\n" + @"__**Help: Config Commands**__

'**{0}config all**' to display all current set configurations __if you are a StormBot administrator__.
'**{0}config prefix [prefix]**' to set the server's bot command prefix __if you are a StormBot administrator__.
'**{0}config toggle bocw**' to enable/disable Black Ops Cold War commands and stat tracking on the server __if you are a StormBot administrator__.
'**{0}config toggle mw**' to enable/disable Modern Warfare commands and stat tracking on the server __if you are a StormBot administrator__.
'**{0}config toggle wz**' to enable/disable Warzone commands and stat tracking on the server __if you are a StormBot administrator__.
'**{0}config toggle sb**' to enable/disable Soundpad commands on the server __if you are a StormBot administrator__.
'**{0}config channel cod [channel]**' to set the server's channel for Call of Duty notifications __if you are a StormBot administrator__.
'**{0}config channel sb [channel]**' to set the server's channel for Soundboard notifications __if you are a StormBot administrator__.
'**{0}config role admin [role]**' to set the server's admin role for special commands and configuration __if you are a StormBot administrator__.
'**{0}config role bocw kills [role]**' to set the server's role for the most weekly Black Ops Cold War kills __if you are a StormBot administrator__.
'**{0}config role mw kills [role]**' to set the server's role for the most weekly Modern Warfare kills __if you are a StormBot administrator__.
'**{0}config role wz wins [role]**' to set the server's role for the most Warzone wins __if you are a StormBot administrator__.
'**{0}config role wz kills [role]**' to set the server's role for the most weekly Warzone kills __if you are a StormBot administrator__.", await GetServerPrefix(_db));
        }

        private async Task<string> HelpSoundboardCommands(bool notAdmin)
        {
            if (await GetServerAllowServerPermissionSoundpadCommands(_db) && await GetServerToggleSoundpadCommands(_db))
            {
                if (DisableIfServiceNotRunning(_soundpadService, "help soundpad"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    return string.Format("\n\n" + @"__**Help: Soundboard Commands**__

'**{0}add [YouTube video URL] [sound name]**' to add a YouTube to MP3 sound to the soundboard in the specified category.
The bot will then ask you to select a category to add the sound to.
'**{0}approve [user]**' to approve a user's existing request to add to the soundboard __if you are a StormBot administrator__.
'**{0}categories**' to display all categories.
'**{0}delete [sound number]**' to delete the sound with the corresponding number from the soundboard __if you are a StormBot administrator__.
'**{0}deny [user]**' to deny a user's existing request to add to the soundboard __if you are a StormBot administrator__.
'**{0}pause**' to pause/resume the sound currently playing.
'**{0}play [sound number]**' to play the sound with the corresponding number.
'**{0}sounds**' to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.
'**{0}sounds [category name]**' to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.
'**{0}stop**' to stop the sound currently playing.", await GetServerPrefix(_db));
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpBlackOpsColdWarCommands(bool notAdmin)
        {
            if (await GetServerAllowServerPermissionBlackOpsColdWarTracking(_db) && await GetServerToggleBlackOpsColdWarTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.BlackOpsColdWarComponent, "help black ops cold war"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string roleStr = "";
                    if (!Context.IsPrivate)
                    {
                        roleStr += " <@&" + (await _callOfDutyService.GetServerBlackOpsColdWarKillsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    return string.Format("\n\n" + @"__**Help: Black Ops Cold War Commands**__

'**{0}bocw participate**' to add your account to the list of Call of Duty accounts participating in the Black Ops Cold War services.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}bocw leave**' to remove your account from the list of Call of Duty accounts participating in the Black Ops Cold War services.
'**{0}bocw participants**' to list out the Call of Duty accounts participating in the Black Ops Cold War services __if you are a StormBot administrator__.
'**{0}bocw add participant [user]**' to add an account to the list of Call of Duty accounts participating in the Black Ops Cold War services __if you are a StormBot administrator__.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}bocw rm participant [user]**' to remove an account from the list of Call of Duty accounts participating in the Black Ops Cold War services __if you are a StormBot administrator__.
'**{0}bocw lifetime kills**' to display the lifetime total game kills of all participating Black Ops Cold War players from highest to lowest __if you are a StormBot administrator__.
'**{0}bocw weekly kills**' to display the total game kills so far this week of all participating Black Ops Cold War players from highest to lowest __if you are a StormBot administrator__.
The bot will only assign the{1} role for Black Ops Cold War kills to the player in first place at the end of the week.", await GetServerPrefix(_db), roleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpModernWarfareCommands(bool notAdmin)
        {
            if (await GetServerAllowServerPermissionModernWarfareTracking(_db) && await GetServerToggleModernWarfareTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.ModernWarfareComponent, "help modern warfare"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string roleStr = "";
                    if (!Context.IsPrivate)
                    {
                        roleStr += " <@&" + (await _callOfDutyService.GetServerModernWarfareKillsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    return string.Format("\n\n" + @"__**Help: Modern Warfare Commands**__

'**{0}mw participate**' to add your account to the list of Call of Duty accounts participating in the Modern Warfare services.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}mw leave**' to remove your account from the list of Call of Duty accounts participating in the Modern Warfare services.
'**{0}mw participants**' to list out the Call of Duty accounts participating in the Modern Warfare services __if you are a StormBot administrator__.
'**{0}mw add participant [user]**' to add an account to the list of Call of Duty accounts participating in the Modern Warfare services __if you are a StormBot administrator__.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}mw rm participant [user]**' to remove an account from the list of Call of Duty accounts participating in the Modern Warfare services __if you are a StormBot administrator__.
'**{0}mw wz lifetime kills**' to display the lifetime total game kills (Modern Warfare + Warzone) of all participating Modern Warfare players from highest to lowest __if you are a StormBot administrator__.
'**{0}mw wz weekly kills**' to display the total game kills (Modern Warfare + Warzone) so far this week of all participating Modern Warfare players from highest to lowest __if you are a StormBot administrator__.
The bot will only assign the{1} role for Modern Warfare kills to the player in first place at the end of the week with the most multiplayer kills (not Warzone).", await GetServerPrefix(_db), roleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpWarzoneCommands(bool notAdmin)
        {
            if (await GetServerAllowServerPermissionWarzoneTracking(_db) && await GetServerToggleWarzoneTracking(_db))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.WarzoneComponent, "help warzone"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string winsRoleStr = "";
                    if (!Context.IsPrivate)
                    {
                        winsRoleStr += " <@&" + (await _callOfDutyService.GetServerWarzoneWinsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    string killsRoleStr = "";
                    if (!Context.IsPrivate)
                    {
                        killsRoleStr += " <@&" + (await _callOfDutyService.GetServerWarzoneKillsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    return string.Format("\n\n" + @"__**Help: Warzone Commands**__

'**{0}wz participate**' to add your account to the list of Call of Duty accounts participating in the Warzone services.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}wz leave**' to remove your account from the list of Call of Duty accounts participating in the Warzone services.
'**{0}wz participants**' to list out the Call of Duty accounts participating in the Warzone services __if you are a StormBot administrator__.
'**{0}wz add participant [user]**' to add an account to the list of Call of Duty accounts participating in the Warzone services __if you are a StormBot administrator__.
The bot will then ask you to enter the account name, tag, and platform.
'**{0}wz rm participant [user]**' to remove an account from the list of Call of Duty accounts participating in the Warzone services __if you are a StormBot administrator__.
'**{0}wz lifetime wins**' to display the lifetime total Warzone wins of all participating players from highest to lowest __if you are a StormBot administrator__.
'**{0}wz weekly wins**' to display the total Warzone wins so far this week of all participating players from highest to lowest __if you are a StormBot administrator__.
The bot will only assign the{1} role for Warzone wins to the player in first place at the end of the week with the most Warzone wins.
The bot will only assign the{2} role for Warzone kills to the player in first place at the end of the week with the most Warzone kills (not multiplayer).", await GetServerPrefix(_db), winsRoleStr, killsRoleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }
    }
}