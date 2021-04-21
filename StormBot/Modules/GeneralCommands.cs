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
                    ServersEntity serverData = BaseService.GetServerEntity(Context.Guild.Id);
                    
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithColor(Color.Blue);
                    builder.WithTitle("**Current Configurations**");
                    builder.WithThumbnailUrl(Context.Guild.IconUrl);
                    builder.AddField("Prefix", $"`{serverData.PrefixUsed}`", false);
                    builder.AddField("Storms Feature", serverData.ToggleStorms ? "`On`" : "`Off`", true);
                    builder.AddField("Market Feature", serverData.ToggleMarket ? "`On`" : "`Off`", true);
                    builder.AddField("Soundboard Feature", serverData.ToggleSoundpadCommands ? "`On`" : "`Off`", true);
                    builder.AddField("Black Ops Cold War Tracking", serverData.ToggleBlackOpsColdWarTracking ? "`On`" : "`Off`", true);
                    builder.AddField("Modern Warfare Tracking", serverData.ToggleModernWarfareTracking ? "`On`" : "`Off`", true);
                    builder.AddField("Warzone Tracking", serverData.ToggleWarzoneTracking ? "`On`" : "`Off`", true);
                    builder.AddField("Storms Channel", $"<#{serverData.StormsNotificationChannelID}>", true);
                    builder.AddField("Soundboard Channel", $"<#{serverData.SoundboardNotificationChannelID}>", true);
                    builder.AddField("Call of Duty Channel", $"<#{serverData.CallOfDutyNotificationChannelID}>", true);
                    builder.AddField("Black Ops Cold War Kills Role", $"<@&{serverData.BlackOpsColdWarKillsRoleID}>", true);
                    builder.AddField("Modern Warfare Kills Role", $"<@&{serverData.ModernWarfareKillsRoleID}>", true);
                    builder.AddField("Warzone Kills Role", $"<@&{serverData.WarzoneKillsRoleID}>", true);
                    builder.AddField("Warzone Wins Role", $"<@&{serverData.WarzoneWinsRoleID}>", true);
                    builder.AddField("Most Storm Resets Role", $"<@&{serverData.StormsMostResetsRoleID}>", true);
                    builder.AddField("Latest Storm Reset Role", $"<@&{serverData.StormsMostRecentResetRoleID}>", true);
                    builder.AddField("Admin Role", $"<@&{serverData.AdminRoleID}>", false);

                    await ReplyAsync("", false, builder.Build());
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

        [Command("subjects", RunMode = RunMode.Async)]
        public async Task SubjectsCommand()
        {
            bool notAdmin = true;
            if (!Context.IsPrivate)
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            if (!notAdmin)
                await Context.Channel.TriggerTypingAsync();

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Blue);
            builder.WithTitle("**Command Categories**");

            if (!Context.IsPrivate)
                builder.WithThumbnailUrl(Context.Guild.IconUrl);

            string subjectsStr = "`Help`\n`Config`\n";

            if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
            {
                if (DisableIfServiceNotRunning(_stormsService, "subjects (storms subject)"))
                {
                    subjectsStr += "`Storms`\n";
                }
            }
            if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
            {
                if (DisableIfServiceNotRunning(_marketService, "subjects (market subject)"))
                {
                    subjectsStr += "`Market`\n";
                }
            }
            if (SoundpadService.GetServerAllowServerPermissionSoundpadCommands(Context) && SoundpadService.GetServerToggleSoundpadCommands(Context))
            {
                if (DisableIfServiceNotRunning(_soundpadService, "subjects (soundpad subject)"))
                {
                    subjectsStr += "`Soundpad`\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionBlackOpsColdWarTracking(Context) && CallOfDutyService.GetServerToggleBlackOpsColdWarTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.BlackOpsColdWarComponent, "subjects (Black Ops Cold War subject)"))
                {
                    subjectsStr += "`Black Ops Cold War`\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionModernWarfareTracking(Context) && CallOfDutyService.GetServerToggleModernWarfareTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.ModernWarfareComponent, "subjects (Modern Warfare subject)"))
                {
                    subjectsStr += "`Modern Warfare`\n";
                }
            }
            if (CallOfDutyService.GetServerAllowServerPermissionWarzoneTracking(Context) && CallOfDutyService.GetServerToggleWarzoneTracking(Context))
            {
                if (DisableIfServiceNotRunning(_callOfDutyService.WarzoneComponent, "subjects (Warzone subject)"))
                {
                    subjectsStr += "`Warzone`\n";
                }
            }

            builder.AddField("Subjects", subjectsStr, false);

            if (!notAdmin)
                await ReplyAsync("", false, builder.Build());
            else
                await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task HelpCommand(params string[] args)
        {
            string subject = GetSingleArg(args);

            bool notAdmin = true;
            if (!Context.IsPrivate)
                notAdmin = !((SocketGuildUser)Context.User).Roles.Select(r => r.Id).Contains(BaseService.GetServerAdminRole(Context.Guild.Id)) && !(((SocketGuildUser)Context.User).GuildPermissions.Administrator);

            string prefix = BaseService.GetServerOrPrivateMessagePrefix(Context);

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Blue);
            builder.WithTitle("**StormBot Commands**");
            builder.WithDescription("Please see https://github.com/cgoulart35/StormBot for more detailed command descriptions.");

            if (!Context.IsPrivate)
                builder.WithThumbnailUrl(Context.Guild.IconUrl);

            if (subject == null)
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                builder.AddField("Help", HelpHelpCommands(prefix));
                builder.AddField("Config", HelpConfigCommands(prefix));

                string commands1 = await HelpStormCommands(notAdmin, prefix);
                if (commands1 != null)
                    builder.AddField("Storm", commands1);

                string commands2 = await HelpMarketCommands(notAdmin, prefix);
                if (commands2 != null)
                    builder.AddField("Market", commands2);

                string commands3 = await HelpSoundboardCommands(notAdmin, prefix);
                if (commands3 != null)
                    builder.AddField("Soundboard", commands3);

                string commands4 = await HelpBlackOpsColdWarCommands(notAdmin, prefix);
                if (commands4 != null)
                    builder.AddField("Black Ops Cold War", commands4);

                string commands5 = await HelpModernWarfareCommands(notAdmin, prefix);
                if (commands5 != null)
                    builder.AddField("Modern Warfare", commands5);

                string commands6 = await HelpWarzoneCommands(notAdmin, prefix);
                if (commands6 != null)
                    builder.AddField("Warzone", commands6);
            }
            else if (subject.ToLower() == "help")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                builder.AddField("Help", HelpHelpCommands(prefix));
            }
            else if (subject.ToLower() == "config" || subject.ToLower() == "configs" || subject.ToLower() == "configuration" || subject.ToLower() == "configurations")
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                builder.AddField("Config", HelpConfigCommands(prefix));
            }
            else if (subject.ToLower() == "storm" || subject.ToLower() == "storms")
            {
                string commands1 = await HelpStormCommands(notAdmin, prefix);
                if (commands1 != null)
                    builder.AddField("Storm", commands1);
            }
            else if (subject.ToLower() == "market")
            {
                string commands2 = await HelpMarketCommands(notAdmin, prefix);
                if (commands2 != null)
                    builder.AddField("Market", commands2);
            }
            else if (subject.ToLower() == "sb" || subject.ToLower() == "sp" || subject.ToLower() == "soundboard" || subject.ToLower() == "soundpad")
            {
                string commands3 = await HelpSoundboardCommands(notAdmin, prefix);
                if (commands3 != null)
                    builder.AddField("Soundboard", commands3);
            }
            else if (subject.ToLower() == "bocw" || subject.ToLower() == "black ops cold war" || subject.ToLower() == "blackopscoldwar" || subject.ToLower() == "cw" || subject.ToLower() == "cold war" || subject.ToLower() == "coldwar")
            {
                string commands4 = await HelpBlackOpsColdWarCommands(notAdmin, prefix);
                if (commands4 != null)
                    builder.AddField("Black Ops Cold War", commands4);
            }
            else if (subject.ToLower() == "mw" || subject.ToLower() == "modern warfare" || subject.ToLower() == "modernwarfare")
            {
                string commands5 = await HelpModernWarfareCommands(notAdmin, prefix);
                if (commands5 != null)
                    builder.AddField("Modern Warfare", commands5);
            }
            else if (subject.ToLower() == "wz" || subject.ToLower() == "warzone")
            {
                string commands6 = await HelpWarzoneCommands(notAdmin, prefix);
                if (commands6 != null)
                    builder.AddField("Warzone", commands6);
            }
            else
            {
                if (!notAdmin)
                    await Context.Channel.TriggerTypingAsync();

                await ReplyAsync("The subject name '" + subject + "' does not exist, or is not available.");

                return;
            }

            if (!notAdmin)
                await ReplyAsync("", false, builder.Build());
            else
                await Context.User.SendMessageAsync("", false, builder.Build());
        }
		#endregion

		private static string HelpHelpCommands(string prefix)
		{
			return string.Format(@"`{0}help`, `{0}help [subject]`, `{0}subjects`", prefix);
		}

		private static string HelpConfigCommands(string prefix)
		{
            return $"`{prefix}config all`, `{prefix}config prefix [prefix]`,\n`{prefix}config toggle bocw`, `{prefix}config toggle mw`, `{prefix}config toggle wz`, `{prefix}config toggle sb`, `{prefix}config toggle storms`,\n`{prefix}config toggle market`,\n`{prefix}config channel cod [channel]`,\n`{prefix}config channel sb [channel]`,\n`{prefix}config channel storms [channel]`,\n`{prefix}config role admin [role]`,\n`{prefix}config role bocw kills [role]`,\n`{prefix}config role mw kills [role]`,\n`{prefix}config role wz wins [role]`,\n`{prefix}config role wz kills [role]`,\n`{prefix}config role storms most [role]`,\n`{prefix}config role storms recent [role]`";
        }

		private async Task<string> HelpStormCommands(bool notAdmin, string prefix)
        {
            if (StormsService.GetServerAllowServerPermissionStorms(Context) && StormsService.GetServerToggleStorms(Context))
            {
                if (DisableIfServiceNotRunning(_stormsService, "help storms"))
                {
                    if (!notAdmin)
                        await Context.Channel.TriggerTypingAsync();

                    return $"`{prefix}umbrella`, `{prefix}guess [number]`, `{prefix}bet [points] [number]`, `{prefix}steal`, `{prefix}insurance`, `{prefix}wallet`, `{prefix}wallets`, `{prefix}resets`";
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

                    return $"`{prefix}craft [imageURL] [price] [item name]`,\n`{prefix}dismantle [item name]`, `{prefix}buy [user] [item name]`,\n`{prefix}sell [user] [item name]`, `{prefix}rename [item name]`,\n`{prefix}item [item name]`, `{prefix}items`, `{prefix}items [user]`";
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

                    return $"`{prefix}add [YouTube video URL] [sound name]`, `{prefix}approve [user]`, `{prefix}categories`, `{prefix}delete [sound number]`, `{prefix}deny [user]`, `{prefix}pause`, `{prefix}play [sound number]`, `{prefix}sounds`, `{prefix}sounds [category name]`, `{prefix}stop`";
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

                    return $"`{prefix}bocw participate`, `{prefix}bocw leave`, `{prefix}bocw participants`,\n`{prefix}bocw add participant [user]`, `{prefix}bocw rm participant [user]`,\n`{prefix}bocw lifetime kills`, `{prefix}bocw weekly kills`";
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

                    return $"`{prefix}mw participate`, `{prefix}mw leave`, `{prefix}mw participants`,\n`{prefix}mw add participant [user]`, `{prefix}mw rm participant [user]`,\n`{prefix}mw wz lifetime kills`, `{prefix}mw wz weekly kills`";
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

                    return $"`{prefix}wz participate`, `{prefix}wz leave`, `{prefix}wz participants`,\n`{prefix}wz add participant [user]`, `{prefix}wz rm participant [user]`,\n`{prefix}wz lifetime wins`, `{prefix}wz weekly wins`";

                }
                else
                    return null;
            }
            else
                return null;
        }
    }
}