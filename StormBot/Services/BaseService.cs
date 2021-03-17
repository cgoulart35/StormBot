using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using StormBot.Database;
using StormBot.Database.Entities;
using StormBot.Models.Enums;

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
		public static async Task AddServerToDatabase(SocketGuild guild)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				ServersEntity newServerData = new ServersEntity()
				{
					ServerID = guild.Id,
					ServerName = guild.Name,
					PrefixUsed = Program.configurationSettingsModel.PrivateMessagePrefix,
					AllowServerPermissionBlackOpsColdWarTracking = true,
					ToggleBlackOpsColdWarTracking = false,
					AllowServerPermissionModernWarfareTracking = true,
					ToggleModernWarfareTracking = false,
					AllowServerPermissionWarzoneTracking = true,
					ToggleWarzoneTracking = false,
					AllowServerPermissionSoundpadCommands = true,
					ToggleSoundpadCommands = false,
					AllowServerPermissionStorms = true,
					ToggleStorms = false,
					CallOfDutyNotificationChannelID = 0,
					SoundboardNotificationChannelID = 0,
					StormsNotificationChannelID = 0,
					AdminRoleID = 0,
					WarzoneWinsRoleID = 0,
					WarzoneKillsRoleID = 0,
					ModernWarfareKillsRoleID = 0,
					BlackOpsColdWarKillsRoleID = 0,
					StormsMostResetsRoleID = 0,
					StormsMostRecentResetRoleID = 0
				};

				_db.Servers.Add(newServerData);
				_db.SaveChanges();
			}
		}

		public static async Task RemoveServerFromDatabase(SocketGuild guild)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				var s = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == guild.Id)
					.AsEnumerable()
					.ToList();

				var c = _db.CallOfDutyPlayerData
					.AsQueryable()
					.Where(c => c.ServerID == guild.Id)
					.AsEnumerable()
					.ToList();

				_db.RemoveRange(s);
				_db.RemoveRange(c);
				_db.SaveChanges();
			}
		}

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

		public static bool SetServerPrefix(ulong serverId, string prefix)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				ServersEntity serverData = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Single();

				if (prefix != serverData.PrefixUsed)
				{
					serverData.PrefixUsed = prefix;
					_db.SaveChanges();
					return true;
				}
				else
					return false;
			}
		}

		public static bool SetServerChannel(ulong serverId, ulong channelId, ServerChannels channelType)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				ServersEntity serverData = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Single();

				bool changed = false;

				switch (channelType)
				{
					case ServerChannels.CallOfDutyNotificationChannel:
						if (serverData.CallOfDutyNotificationChannelID != channelId)
						{
							serverData.CallOfDutyNotificationChannelID = channelId;
							changed = true;
						}
						break;
					case ServerChannels.SoundboardNotificationChannel:
						if (serverData.SoundboardNotificationChannelID != channelId)
						{
							serverData.SoundboardNotificationChannelID = channelId;
							changed = true;
						}
						break;
					case ServerChannels.StormsNotificationChannel:
						if (serverData.StormsNotificationChannelID != channelId)
						{
							serverData.StormsNotificationChannelID = channelId;
							changed = true;
						}
						break;
				}

				if (changed)
					_db.SaveChanges();

				return changed;
			}
		}

		public static bool SetServerRole(ulong serverId, ulong roleId, ServerRoles roleType)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				ServersEntity serverData = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Single();

				bool changed = false;

				switch (roleType)
				{
					case ServerRoles.AdminRole:
						if (serverData.AdminRoleID != roleId)
						{
							serverData.AdminRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.WarzoneWinsRole:
						if (serverData.WarzoneWinsRoleID != roleId)
						{
							serverData.WarzoneWinsRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.WarzoneKillsRole:
						if (serverData.WarzoneKillsRoleID != roleId)
						{
							serverData.WarzoneKillsRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.ModernWarfareKillsRole:
						if (serverData.ModernWarfareKillsRoleID != roleId)
						{
							serverData.ModernWarfareKillsRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.BlackOpsColdWarKillsRole:
						if (serverData.BlackOpsColdWarKillsRoleID != roleId)
						{
							serverData.BlackOpsColdWarKillsRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.StormsMostResetsRole:
						if (serverData.StormsMostResetsRoleID != roleId)
						{
							serverData.StormsMostResetsRoleID = roleId;
							changed = true;
						}
						break;
					case ServerRoles.StormsMostRecentResetRole:
						if (serverData.StormsMostRecentResetRoleID != roleId)
						{
							serverData.StormsMostRecentResetRoleID = roleId;
							changed = true;
						}
						break;
				}

				if (changed)
					_db.SaveChanges();

				return changed;
			}
		}

		public static bool? ToggleServerService(ulong serverId, ServerServices serviceType)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				ServersEntity serverData = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Single();

				bool? flag = null;

				switch (serviceType)
				{
					case ServerServices.BlackOpsColdWarService:
						flag = serverData.ToggleBlackOpsColdWarTracking;
						serverData.ToggleBlackOpsColdWarTracking = !flag.Value;
						break;
					case ServerServices.ModernWarfareService:
						flag = serverData.ToggleModernWarfareTracking;
						serverData.ToggleModernWarfareTracking = !flag.Value;
						break;
					case ServerServices.WarzoneService:
						flag = serverData.ToggleWarzoneTracking;
						serverData.ToggleWarzoneTracking = !flag.Value;
						break;
					case ServerServices.SoundpadService:
						flag = serverData.ToggleSoundpadCommands;
						serverData.ToggleSoundpadCommands = !flag.Value;
						break;
					case ServerServices.StormsService:
						flag = serverData.ToggleStorms;
						serverData.ToggleStorms = !flag.Value;
						break;
					default:
						return flag;
				}

				_db.SaveChanges();
				return !flag.Value;
			}
		}
		#endregion
	}
}
