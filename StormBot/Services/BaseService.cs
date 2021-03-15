using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class BaseService
	{
		public string Name { get; set; }

		public bool DoStart { get; set; }

		public bool IsServiceRunning { get; set; }

		public virtual async Task StartService()
		{
			string logStamp = GetLogStamp();

			if (!DoStart)
			{
				Console.WriteLine(logStamp + "Disabled.".PadLeft(60 - logStamp.Length));
			}
			else if (IsServiceRunning)
			{
				Console.WriteLine(logStamp + "Service already running.".PadLeft(75 - logStamp.Length));
			}
			else
			{
				Console.WriteLine(logStamp + "Starting service.".PadLeft(68 - logStamp.Length));

				IsServiceRunning = true;
			}
		}

		public virtual async Task StopService()
		{
			string logStamp = GetLogStamp();

			if (IsServiceRunning)
			{
				Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

				IsServiceRunning = false;
			}
		}

		public string GetLogStamp()
		{
			return DateTime.Now.ToString("HH:mm:ss ") + Name;
		}

		public static async Task UnassignRoleFromAllMembers(ulong roleID, SocketGuild guild)
		{
			var role = guild.GetRole(roleID);
			IEnumerable<SocketGuildUser> roleMembers = guild.GetRole(roleID).Members;
			foreach (SocketGuildUser roleMember in roleMembers)
			{
				await roleMember.RemoveRoleAsync(role);
			}
		}

		public static async Task GiveUsersRole(ulong roleID, List<ulong> discordIDs, SocketGuild guild)
		{
			var role = guild.GetRole(roleID);

			foreach (ulong discordID in discordIDs)
			{
				var roleMember = guild.GetUser(discordID);
				await roleMember.AddRoleAsync(role);
			}
		}

		#region QUERIES
		public static List<ServersEntity> GetAllServerEntities()
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
				.AsQueryable()
				.AsEnumerable()
				.ToList();
			}
		}

		public static ServersEntity GetServerEntity(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Single();
			}
		}

		public static ulong GetServerAdminRole(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Select(s => s.AdminRoleID)
				.Single();
			}
		}

		public static string GetServerPrefix(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == serverId)
				.Select(s => s.PrefixUsed)
				.Single();
			}
		}

		public static string GetServerOrPrivateMessagePrefix(SocketCommandContext context)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				if (!context.IsPrivate)
				{
					return _db.Servers
						.AsQueryable()
						.Where(s => s.ServerID == context.Guild.Id)
						.Select(s => s.PrefixUsed)
						.Single();
				}
				else
				{
					return Program.configurationSettingsModel.PrivateMessagePrefix;
				}
			}
		}
		#endregion
	}
}
