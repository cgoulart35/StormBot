﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.Interactive;
using Discord.Commands;
using cg_bot.Services;
using cg_bot.Database;

namespace cg_bot.Modules
{
	public class BaseCommand : InteractiveBase<SocketCommandContext>
	{
        public string GetSingleArg(string[] args)
		{
			return args.Length != 0 ? string.Join(" ", args) : null;
		}

        public ulong GetDiscordID(string input, bool isUser = true, bool isChannel = true)
        {
            // if no name given
            if (input == null)
                throw new Exception();

            string trimmedInput = "";

            // if username is in <@!xxxxxxxxxxxxxxxxxx> format
            if (input.Length == 22)
                trimmedInput = input.Substring(3, 18);

            // if username is in <@xxxxxxxxxxxxxxxxxx> format
            else if (input.Length == 21)
                trimmedInput = input.Substring(2, 18);

            ulong discordID = Convert.ToUInt64(trimmedInput);

            if (isUser)
            {
                // if user exists in the server
                if (Context.Guild.GetUser(discordID) == null)
                    throw new Exception();
            }
            else
            {
                if (isChannel)
                {
                    // if channel exists in the server
                    if (Context.Guild.GetChannel(discordID) == null)
                        throw new Exception();
                }
                else
                {
                    // if role exists in the server
                    if (Context.Guild.GetRole(discordID) == null)
                        throw new Exception();
                }
            }

            return discordID;
        }

        public bool DisableIfServiceNotRunning(BaseService service, string command = null)
        {
            string serviceName = service.Name;
            bool isRunning = service.isServiceRunning;
            if (isRunning == false)
            {
                // if the call is being used to disable a command
                if (command != null)
                {
                    Console.WriteLine($"Command [{command}] will be ignored: {serviceName} not running.");
                }
                
                // disabled
                return false;
            }
            else
            {
                // enabled
                return true;
            }
        }

        public List<string> ValidateOutputLimit(List<string> output, string messageToAdd)
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

        public async Task<string> GetServerPrefix(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                return await _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == Context.Guild.Id)
                    .Select(s => s.PrefixUsed)
                    .SingleAsync();
            }
            else
            {
                return Program.configurationSettingsModel.PrivateMessagePrefix;
            }
        }

        public async Task<ulong> GetServerAdminRole(CgBotContext _db)
        {
            return await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.AdminRoleID)
                .SingleAsync();
        }

        public async Task<bool> GetServerToggleBlackOpsColdWarTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == Context.Guild.Id)
                    .Select(s => s.ToggleBlackOpsColdWarTracking)
                    .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerToggleModernWarfareTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.ToggleModernWarfareTracking)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerToggleWarzoneTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.ToggleModernWarfareTracking)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerToggleSoundpadCommands(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.ToggleSoundpadCommands)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerAllowServerPermissionBlackOpsColdWarTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.AllowServerPermissionBlackOpsColdWarTracking)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerAllowServerPermissionModernWarfareTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.AllowServerPermissionModernWarfareTracking)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerAllowServerPermissionWarzoneTracking(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.AllowServerPermissionWarzoneTracking)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }

        public async Task<bool> GetServerAllowServerPermissionSoundpadCommands(CgBotContext _db)
        {
            if (!Context.IsPrivate)
            {
                bool flag = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == Context.Guild.Id)
                .Select(s => s.AllowServerPermissionSoundpadCommands)
                .SingleAsync();

                if (!flag)
                    Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {Context.Guild.Name} ({Context.Guild.Id})");

                return flag;
            }
            else
                return true;
        }
    }
}
