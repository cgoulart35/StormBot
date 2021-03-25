using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Services;
using StormBot.Database.Entities;
using StormBot.Models.Enums;

namespace StormBot.Modules
{
	public class GeneralCommands : BaseCommand
    {
        private readonly StormsService _stormsService;
        private readonly MarketService _marketService;
        private readonly SoundpadService _soundpadService;
        private readonly CallOfDutyService _callOfDutyService;

        public GeneralCommands(IServiceProvider services)
        {
            _stormsService = services.GetRequiredService<StormsService>();
            _marketService = services.GetRequiredService<MarketService>();
            _soundpadService = services.GetRequiredService<SoundpadService>();
            _callOfDutyService = services.GetRequiredService<CallOfDutyService>();
        }

        #region COMMAND FUNCTIONS
        // admin role command
        [Command("config all", RunMode = RunMode.Async)]
        public async Task ConfigAllCommand()
        {
            await Context.Channel.TriggerTypingAsync();

            if (!Context.IsPrivate)
            {
                if (!(((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id))) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                {
                    await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                }
                else
                {
                    string message;

                    ServersEntity serverData = BaseService.GetServerEntity(Context.Guild.Id);

                    message = string.Format(@"__**Current Configurations:**__

**Prefix:** {0}
**Black Ops Cold War Tracking Feature:** {1}
**Modern Warfare Tracking Feature:** {2}
**Warzone Tracking Feature:** {3}
**Soundboard Feature:** {4}
**Storms Feature:** {5}
**Market Feature:** {6}
**Call of Duty notification channel:** <#{7}>
**Soundboard notification channel:** <#{8}>
**Storms notification channel:** <#{9}>
**Admin role:** <@&{10}>
**Black Ops Cold War kills role:** <@&{11}>
**Modern Warfare kills role:** <@&{12}>
**Warzone wins role:** <@&{13}>
**Warzone kills role:** <@&{14}>
**Most Storm resets role:** <@&{15}>
**Latest Storm reset role:** <@&{16}>", serverData.PrefixUsed, serverData.ToggleBlackOpsColdWarTracking ? "On" : "Off", serverData.ToggleModernWarfareTracking ? "On" : "Off", serverData.ToggleWarzoneTracking ? "On" : "Off", serverData.ToggleSoundpadCommands ? "On" : "Off", serverData.ToggleStorms ? "On" : "Off", serverData.ToggleMarket ? "On" : "Off", serverData.CallOfDutyNotificationChannelID, serverData.SoundboardNotificationChannelID, serverData.StormsNotificationChannelID, serverData.AdminRoleID, serverData.BlackOpsColdWarKillsRoleID, serverData.ModernWarfareKillsRoleID, serverData.WarzoneWinsRoleID, serverData.WarzoneKillsRoleID, serverData.StormsMostResetsRoleID, serverData.StormsMostRecentResetRoleID);

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
                if (!(((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id))) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                {
                    await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                }
                else
                {
                    if (args.Length == 1)
                    {
                        string prefix = GetSingleArg(args);

                        if (BaseService.SetServerPrefix(Context.Guild.Id, prefix))
                            await ReplyAsync("The server prefix was set to: " + BaseService.GetServerOrPrivateMessagePrefix(Context));
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
                if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.BlackOpsColdWarService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Black Ops Cold War tracking was enabled.");
                            else
                                await ReplyAsync("Black Ops Cold War tracking was disabled.");
                        }
                        else
                        {
                            if (CallOfDutyService.GetServerCallOfDutyNotificationChannel(Context.Guild.Id) == null)
                                await ReplyAsync("Please configure a channel to use for Call Of Duty.");
                            if (CallOfDutyService.GetServerBlackOpsColdWarKillsRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for Black Ops Cold War kills.");
                        }
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
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.ModernWarfareService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Modern Warfare tracking was enabled.");
                            else
                                await ReplyAsync("Modern Warfare tracking was disabled.");
                        }
                        else
                        {
                            if (CallOfDutyService.GetServerCallOfDutyNotificationChannel(Context.Guild.Id) == null)
                                await ReplyAsync("Please configure a channel to use for Call Of Duty.");
                            if (CallOfDutyService.GetServerModernWarfareKillsRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for Modern Warfare kills.");
                        }
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
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.WarzoneService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Warzone tracking was enabled.");
                            else
                                await ReplyAsync("Warzone tracking was disabled.");
                        }
                        else
                        {
                            if (CallOfDutyService.GetServerCallOfDutyNotificationChannel(Context.Guild.Id) == null)
                                await ReplyAsync("Please configure a channel to use for Call Of Duty.");
                            if (CallOfDutyService.GetServerWarzoneKillsRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for Warzone kills.");
                            if (CallOfDutyService.GetServerWarzoneWinsRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for Warzone wins.");
                        }
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
                if (SoundpadService.GetServerAllowServerPermissionSoundpadCommands(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.SoundpadService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Soundboard commands were enabled.");
                            else
                                await ReplyAsync("Soundboard commands were disabled.");
                        }
                        else
                            await ReplyAsync("Please configure a channel to use for Soundpad.");
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config toggle storms", RunMode = RunMode.Async)]
        public async Task ConfigToggleStormsCommand()
        {
            if (!Context.IsPrivate)
            {
                if (StormsService.GetServerAllowServerPermissionStorms(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.StormsService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Storms were enabled.");
                            else
                                await ReplyAsync("Storms were disabled.");
                        }
                        else
                        {
                            if (StormsService.GetServerStormsNotificationChannel(Context.Guild.Id) == null)
                                await ReplyAsync("Please configure a channel to use for Storms.");
                            if (StormsService.GetStormsMostRecentResetRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for the most recent Storm reset.");
                            if (StormsService.GetStormsMostResetsRoleID(Context.Guild.Id) == 0)
                                await ReplyAsync("Please configure a role to use for the most Storm resets.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        // admin role command
        [Command("config toggle market", RunMode = RunMode.Async)]
        public async Task ConfigToggleMarketCommand()
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        bool? flag = await BaseService.ToggleServerService(Context.Guild.Id, ServerServices.MarketService);

                        if (flag.HasValue)
                        {
                            if (flag.Value)
                                await ReplyAsync("Market was enabled.");
                            else
                                await ReplyAsync("Market was disabled.");
                        }
                        else
                        {
                            if (StormsService.GetServerToggleStorms(Context) == false)
                                await ReplyAsync("Please enable Storms.");
                        }
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
                if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context) || CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) || CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordChannelID = GetDiscordID(input, false);

                            bool changed = await BaseService.SetServerChannel(Context.Guild.Id, discordChannelID, ServerChannels.CallOfDutyNotificationChannel);

                            if (changed)
                                await ReplyAsync($"The Call of Duty notification channel has been set to: <#{discordChannelID}>");
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
                if (SoundpadService.GetServerAllowServerPermissionSoundpadCommands(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordChannelID = GetDiscordID(input, false);

                            bool changed = await BaseService.SetServerChannel(Context.Guild.Id, discordChannelID, ServerChannels.SoundboardNotificationChannel);

                            if (changed)
                                await ReplyAsync($"The Soundboard notification channel has been set to: <#{discordChannelID}>");
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
        [Command("config channel storms", RunMode = RunMode.Async)]
        public async Task ConfigChannelStormsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (StormsService.GetServerAllowServerPermissionStorms(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordChannelID = GetDiscordID(input, false);

                            bool changed = await BaseService.SetServerChannel(Context.Guild.Id, discordChannelID, ServerChannels.StormsNotificationChannel);

                            if (changed)
                                await ReplyAsync($"The Storm notification channel has been set to: <#{discordChannelID}>");
                            else
                                await ReplyAsync("The server is already using this channel for Storm notifications.");
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
                await Context.Channel.TriggerTypingAsync();

                if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                {
                    await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                }
                else
                {
                    try
                    {
                        string input = GetSingleArg(args);
                        ulong discordRoleID = GetDiscordID(input, false, false);

                        bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.AdminRole);

                        if (changed)
                            await ReplyAsync($"The admin role has been set to: <@&{discordRoleID}>");
                        else
                            await ReplyAsync("The server is already using this role as the admin role.");
                    }
                    catch
                    {
                        await ReplyAsync("Please provide a valid Discord role.");
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
                if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.BlackOpsColdWarKillsRole);

                            if (changed)
                                await ReplyAsync($"The Black Ops Cold War kills role has been set to: <@&{discordRoleID}>");
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
                if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.ModernWarfareKillsRole);

                            if (changed)
                                await ReplyAsync($"The Modern Warfare kills role has been set to: <@&{discordRoleID}>");
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
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.WarzoneWinsRole);

                            if (changed)
                                await ReplyAsync($"The Warzone wins role has been set to: <@&{discordRoleID}>");
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
                if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.WarzoneKillsRole);

                            if (changed)
                                await ReplyAsync($"The Warzone kills role has been set to: <@&{discordRoleID}>");
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

        // admin role command
        [Command("config role storms most", RunMode = RunMode.Async)]
        public async Task ConfigRoleStormMostCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (StormsService.GetServerAllowServerPermissionStorms(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.StormsMostResetsRole);

                            if (changed)
                                await ReplyAsync($"The Storms role for the most resets has been set to: <@&{discordRoleID}>");
                            else
                                await ReplyAsync("The server is already using this role for the most Storm resets.");
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
        [Command("config role storms recent", RunMode = RunMode.Async)]
        public async Task ConfigRoleStormRecentCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (StormsService.GetServerAllowServerPermissionStorms(Context))
                {
                    await Context.Channel.TriggerTypingAsync();

                    if (!((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator))
                    {
                        await ReplyAsync($"Sorry <@!{Context.User.Id}>, only StormBot Administrators can run this command.");
                    }
                    else
                    {
                        try
                        {
                            string input = GetSingleArg(args);
                            ulong discordRoleID = GetDiscordID(input, false, false);

                            bool changed = BaseService.SetServerRole(Context.Guild.Id, discordRoleID, ServerRoles.StormsMostRecentResetRole);

                            if (changed)
                                await ReplyAsync($"The Storms role for the most recent reset has been set to: <@&{discordRoleID}>");
                            else
                                await ReplyAsync("The server is already using this role for the most recent Storm reset.");
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
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            string prefix = BaseService.GetServerOrPrivateMessagePrefix(Context);

            if (subject == null)
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                output = ValidateOutputLimit(output, HelpHelpCommands(prefix));
                output = ValidateOutputLimit(output, HelpConfigCommands(false, prefix));
                output = ValidateOutputLimit(output, HelpConfigCommands(true, prefix));
                output = ValidateOutputLimit(output, await HelpStormCommands(notAdmin, prefix));
                output = ValidateOutputLimit(output, await HelpMarketCommands(notAdmin, prefix));
                output = ValidateOutputLimit(output, await HelpSoundboardCommands(notAdmin, prefix));
                output = ValidateOutputLimit(output, await HelpBlackOpsColdWarCommands(notAdmin, prefix));
                output = ValidateOutputLimit(output, await HelpModernWarfareCommands(notAdmin, prefix));
                output = ValidateOutputLimit(output, await HelpWarzoneCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "help")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();
                output = ValidateOutputLimit(output, HelpHelpCommands(prefix));
            }
            else if (subject.ToLower() == "config" || subject.ToLower() == "configs" || subject.ToLower() == "configuration" || subject.ToLower() == "configurations")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();
                output = ValidateOutputLimit(output, HelpConfigCommands(false, prefix));
                output = ValidateOutputLimit(output, HelpConfigCommands(true, prefix));
            }
            else if (subject.ToLower() == "storm" || subject.ToLower() == "storms")
            {
                output = ValidateOutputLimit(output, await HelpStormCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "market")
            {
                output = ValidateOutputLimit(output, await HelpMarketCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "sb" || subject.ToLower() == "sp" || subject.ToLower() == "soundboard" || subject.ToLower() == "soundpad")
            {
                output = ValidateOutputLimit(output, await HelpSoundboardCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "mw" || subject.ToLower() == "modern warfare" || subject.ToLower() == "modernwarfare")
            {
                output = ValidateOutputLimit(output, await HelpModernWarfareCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "wz" || subject.ToLower() == "warzone")
            {
                output = ValidateOutputLimit(output, await HelpWarzoneCommands(notAdmin, prefix));
            }
            else if (subject.ToLower() == "bocw" || subject.ToLower() == "black ops cold war" || subject.ToLower() == "blackopscoldwar" || subject.ToLower() == "cw" || subject.ToLower() == "cold war" || subject.ToLower() == "coldwar")
            {
                output = ValidateOutputLimit(output, await HelpBlackOpsColdWarCommands(notAdmin, prefix));
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
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            if (!notAdmin)
                await Context.Channel.TriggerTypingAsync();

            string output = "__**Subjects:**__\nHelp\nConfig\n";

            if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
            {
                if (DisableIfServiceNotRunning(_stormsService, "subjects (storms subject)"))
                {
                    output += "Storms\n";
                }
            }
            if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
            {
                if (DisableIfServiceNotRunning(_marketService, "subjects (market subject)"))
                {
                    output += "Market\n";
                }
            }
            if (SoundpadService.GetServerAllowServerPermissionSoundpadCommands(Context) && SoundpadService.GetServerToggleSoundpadCommands(Context))
            {
                if (DisableIfServiceNotRunning(_soundpadService, "subjects (soundpad subject)"))
                {
                    output += "Soundpad\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context) && CallOfDutyService.GetServerToggleBlackOpsColdWarTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.BlackOpsColdWarComponent, "subjects (Black Ops Cold War subject)"))
                {
                    output += "Black Ops Cold War\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.ModernWarfareComponent, "subjects (Modern Warfare subject)"))
                {
                    output += "Modern Warfare\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
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

		private static string HelpHelpCommands(string prefix)
		{
			return string.Format("\n\n" + @"__**Help: Help Commands**__

'**{0}help**' to display information on all commands.
'**{0}help [subject]**' to display information on all commands for a specific subject.
'**{0}subjects**' to display the existing command subjects.", prefix);
		}

		private static string HelpConfigCommands(bool partTwo, string prefix)
		{
			if (!partTwo)
			{
				return string.Format("\n\n" + @"__**Help: Config Commands**__

'**{0}config all**' to display all current set configurations __if you are a StormBot administrator__.
'**{0}config prefix [prefix]**' to set the server's bot command prefix __if you are a StormBot administrator__.
'**{0}config toggle bocw**' to enable/disable Black Ops Cold War commands and stat tracking on the server __if you are a StormBot administrator__.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Black Ops Cold War participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).
'**{0}config toggle mw**' to enable/disable Modern Warfare commands and stat tracking on the server __if you are a StormBot administrator__.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Modern Warfare participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).
'**{0}config toggle wz**' to enable/disable Warzone commands and stat tracking on the server __if you are a StormBot administrator__.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Warzone participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).
'**{0}config toggle sb**' to enable/disable Soundpad commands on the server __if you are a StormBot administrator__.
'**{0}config toggle storms**' to enable/disable Storms and reactive commands on the server __if you are a StormBot administrator__.
'**{0}config toggle market**' to enable/disable Market commands on the server __if you are a StormBot administrator__.", prefix);
			}
			else
			{
				return string.Format("\n\n" + @"'**{0}config channel cod [channel]**' to set the server's channel for Call of Duty notifications __if you are a StormBot administrator__.
'**{0}config channel sb [channel]**' to set the server's channel for Soundboard notifications __if you are a StormBot administrator__.
'**{0}config channel storms [channel]**' to set the server's channel for Storm notifications __if you are a StormBot administrator__.
'**{0}config role admin [role]**' to set the server's admin role for special commands and configuration __if you are a StormBot administrator__.
'**{0}config role bocw kills [role]**' to set the server's role for the most weekly Black Ops Cold War kills __if you are a StormBot administrator__.
'**{0}config role mw kills [role]**' to set the server's role for the most weekly Modern Warfare kills __if you are a StormBot administrator__.
'**{0}config role wz wins [role]**' to set the server's role for the most Warzone wins __if you are a StormBot administrator__.
'**{0}config role wz kills [role]**' to set the server's role for the most weekly Warzone kills __if you are a StormBot administrator__.
'**{0}config role storms most [role]**' to set the server's role for the most Storm resets __if you are a StormBot administrator__.
'**{0}config role storms recent [role]**' to set the server's role for the most recent Storm reset __if you are a StormBot administrator__.", prefix);
			}
		}

		private async Task<string> HelpStormCommands(bool notAdmin, string prefix)
        {
            if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
            {
                if (DisableIfServiceNotRunning(_stormsService, "help storms"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string mostRecentRoleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        mostRecentRoleStr += " <@&" + (StormsService.GetStormsMostRecentResetRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    string mostResetsRoleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        mostResetsRoleStr += " <@&" + (StormsService.GetStormsMostResetsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    return string.Format("\n\n" + @"__**Help: Storm Commands**__

'**{0}umbrella**' to start the incoming Storm and earn {1} points.
'**{0}guess [number]**' to make a guess with a winning reward of {2} points. (__during Storm only__)
'**{0}bet [points] [number]**' to make a guess. If you win, you earn the amount of points bet within your wallet. If you lose, you lose those points. (__during Storm only__)
'**{0}steal**' to steal {3} points from the player with the most points. (__during Storm only__)
'**{0}buy insurance**' to buy insurance for {4} points to protect your wallet from disasters.
'**{0}wallet**' to show how many points you have in your wallet.
'**{0}wallets**' to show how many points everyone has.
'**{0}resets**' to show how many resets everyone has.

The bot will assign the{5} role for the most recent reset to the player who causes the next reset by reaching {6} points.
The bot will also assign the{7} role for the most total resets to the players in the lead.
Wallets are reset to {8} points when a disaster happens to a them once someone reaches {9} points, or if a reset occurs.", prefix, StormsService.levelOneReward, StormsService.levelTwoReward, StormsService.stealAmount, StormsService.insuranceCost, mostRecentRoleStr, StormsService.resetMark, mostResetsRoleStr, StormsService.resetBalance, StormsService.disasterMark);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpMarketCommands(bool notAdmin, string prefix)
        {
            if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
            {
                if (DisableIfServiceNotRunning(_marketService, "help market"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    return string.Format("\n\n" + @"__**Help: Market Commands**__

'**{0}craft [imageURL] [price] [item name]**' to craft a market item in your inventory. You will be charged 20% of the sale price for manufacturing.
'**{0}dismantle [item name]**' to destroy an item for points. Only items that have been sold can be dismantled.
'**{0}buy [user] [item name]**' to request to buy a user's item for its listed price.
'**{0}sell [user] [item name]**' to sell your item to the requesting user at the listed price.
'**{0}rename [item name]**' to rename your item to a different name. The bot will then ask what you would like to change the item's name to.
'**{0}item [item name]**' to show off your item.
'**{0}items**' to list out your items.
'**{0}items [user]**' to list out a user's items.

Market items can't be sold for higher than the reset mark ({1} points). You cannot possess more than one item with the same name. You cannot sell or buy items to or from yourself.", prefix, StormsService.resetMark);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpSoundboardCommands(bool notAdmin, string prefix)
        {
            if (SoundpadService.GetServerAllowServerPermissionSoundpadCommands(Context) && SoundpadService.GetServerToggleSoundpadCommands(Context))
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
'**{0}stop**' to stop the sound currently playing.", prefix);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpBlackOpsColdWarCommands(bool notAdmin, string prefix)
        {
            if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context) && CallOfDutyService.GetServerToggleBlackOpsColdWarTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.BlackOpsColdWarComponent, "help black ops cold war"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string roleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        roleStr += " <@&" + (CallOfDutyService.GetServerBlackOpsColdWarKillsRoleID(Context.Guild.Id)).ToString() + ">";
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

The bot will only assign the{1} role for Black Ops Cold War kills to the player in first place at the end of the week.", prefix, roleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpModernWarfareCommands(bool notAdmin, string prefix)
        {
            if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.ModernWarfareComponent, "help modern warfare"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string roleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        roleStr += " <@&" + (CallOfDutyService.GetServerModernWarfareKillsRoleID(Context.Guild.Id)).ToString() + ">";
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

The bot will only assign the{1} role for Modern Warfare kills to the player in first place at the end of the week with the most multiplayer kills (not Warzone).", prefix, roleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }

        private async Task<string> HelpWarzoneCommands(bool notAdmin, string prefix)
        {
            if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.WarzoneComponent, "help warzone"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    string winsRoleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        winsRoleStr += " <@&" + (CallOfDutyService.GetServerWarzoneWinsRoleID(Context.Guild.Id)).ToString() + ">";
                    }

                    string killsRoleStr = "";
                    if (!Context.IsPrivate && !notAdmin)
                    {
                        killsRoleStr += " <@&" + (CallOfDutyService.GetServerWarzoneKillsRoleID(Context.Guild.Id)).ToString() + ">";
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
The bot will only assign the{2} role for Warzone kills to the player in first place at the end of the week with the most Warzone kills (not multiplayer).", prefix, winsRoleStr, killsRoleStr);
                }
                else
                    return null;
            }
            else
                return null;
        }
    }
}