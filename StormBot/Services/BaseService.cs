using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class BaseService
	{
		public string Name { get; set; }

		public bool DoStart { get; set; }

		public bool isServiceRunning { get; set; }

		public static StormBotContext _db;

		public static readonly object queryLock = new object();

		public BaseService(IServiceProvider services)
		{
			_db = services.GetRequiredService<StormBotContext>();
		}

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
		public List<ServersEntity> GetAllServerEntities()
		{
			lock (BaseService.queryLock)
			{
				return _db.Servers
				.AsQueryable()
				.AsEnumerable()
				.ToList();
			}
		}

		public ServersEntity GetServerEntity(ulong serverId)
		{
			lock (BaseService.queryLock)
			{
				return _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Single();
			}
		}

		public string GetServerPrefix(ulong serverId)
		{
			lock (BaseService.queryLock)
			{
				return _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Select(s => s.PrefixUsed)
				.Single();
			}
		}
		#endregion
	}
}
