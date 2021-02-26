using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class BaseService
	{
		public string Name { get; set; }

		public bool DoStart { get; set; }

		public bool isServiceRunning { get; set; }

		public StormBotContext _db { get; set; }

		public virtual async Task StartService()
		{
			string logStamp = GetLogStamp();

			if (!DoStart)
			{
				Console.WriteLine(logStamp + "Disabled.".PadLeft(60 - logStamp.Length));
			}
			else if (isServiceRunning)
			{
				Console.WriteLine(logStamp + "Service already running.".PadLeft(75 - logStamp.Length));
			}
			else
			{
				Console.WriteLine(logStamp + "Starting service.".PadLeft(68 - logStamp.Length));

				isServiceRunning = true;
			}
		}

		public virtual async Task StopService()
		{
			string logStamp = GetLogStamp();

			if (isServiceRunning)
			{
				Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

				isServiceRunning = false;
			}
		}

		public string GetLogStamp()
		{
			return DateTime.Now.ToString("HH:mm:ss ") + Name;
		}

		public async Task UnassignRoleFromAllMembers(ulong roleID, SocketGuild guild)
		{
			var role = guild.GetRole(roleID);
			IEnumerable<SocketGuildUser> roleMembers = guild.GetRole(roleID).Members;
			foreach (SocketGuildUser roleMember in roleMembers)
			{
				await roleMember.RemoveRoleAsync(role);
			}
		}

		public async Task GiveUsersRole(ulong roleID, List<ulong> discordIDs, SocketGuild guild)
		{
			var role = guild.GetRole(roleID);

			foreach (ulong discordID in discordIDs)
			{
				var roleMember = guild.GetUser(discordID);
				await roleMember.AddRoleAsync(role);
			}
		}

		#region QUERIES
		public async Task<List<ServersEntity>> GetAllServerEntities()
		{
			return await _db.Servers
				.AsQueryable()
				.AsAsyncEnumerable()
				.ToListAsync();
		}

		public async Task<string> GetServerPrefix(ulong serverId)
		{
			return await _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Select(s => s.PrefixUsed)
				.SingleAsync();
		}
		#endregion
	}
}
