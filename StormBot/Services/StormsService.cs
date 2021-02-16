using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StormBot.Database;
using StormBot.Database.Entities;
using Discord.Commands;

namespace StormBot.Services
{
	public class StormsService : BaseService
	{
		public readonly DiscordSocketClient _client;

		private Dictionary<ulong, int> OngoingStormsLevel;
		private Dictionary<ulong, int> OngoingStormsWinningNumber;

		private Random random;

		public Emoji cloud_rain;
		public Emoji thunder_cloud_rain;
		public Emoji umbrella2;
		public Emoji white_sun_rain_cloud;
		public Emoji sun_with_face;

		public int levelOneReward = 1;
		public int levelTwoReward = 2;

		public List<IUserMessage> purgeCollection;

		public StormsService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_db = services.GetRequiredService<StormBotContext>();

			Name = "Storm Service";
			isServiceRunning = false;

			OngoingStormsLevel = new Dictionary<ulong, int>();
			OngoingStormsWinningNumber = new Dictionary<ulong, int>();

			random = new Random();

			cloud_rain = new Emoji("🌧️");
			thunder_cloud_rain = new Emoji("⛈️");
			umbrella2 = new Emoji("☂️");
			white_sun_rain_cloud = new Emoji("🌦️");
			sun_with_face = new Emoji("🌞");
			
			levelOneReward = 1;
			levelTwoReward = 2;

			purgeCollection = new List<IUserMessage>();
	}

		public override async Task StartService()
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

				List<ServersEntity> servers = await GetAllServerEntities();
				foreach (ServersEntity server in servers)
				{
					string message = "";

					if (isServiceRunning && server.AllowServerPermissionStorms && server.ToggleStorms)
					{
						message += "_**[    STORMS ONLINE.    ]**_\n";
					}

					if (server.StormsNotificationChannelID != 0 && message != "")
						await ((IMessageChannel)_client.GetChannel(server.StormsNotificationChannelID)).SendMessageAsync(message);
				}
			}
		}

		public override async Task StopService()
		{
			string logStamp = GetLogStamp();

			if (isServiceRunning)
			{
				Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

				List<ServersEntity> servers = await GetAllServerEntities();
				foreach (ServersEntity server in servers)
				{
					string message = "";

					if (isServiceRunning && server.AllowServerPermissionStorms && server.ToggleStorms)
					{
						message += "_**[    STORMS OFFLINE.    ]**_\n";
					}

					if (server.StormsNotificationChannelID != 0 && message != "")
						await ((IMessageChannel)_client.GetChannel(server.StormsNotificationChannelID)).SendMessageAsync(message);
				}

				isServiceRunning = false;
			}
		}

		public async Task HandleIncomingStorm(object sender, ulong serverId, ulong channelId)
		{
			int randomNumber = random.Next(1, 201);

			OngoingStormsLevel.Add(channelId, 1);
			OngoingStormsWinningNumber.Add(channelId, randomNumber);

			purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(cloud_rain.ToString() + thunder_cloud_rain.ToString() + umbrella2.ToString() + " __**STORM INCOMING**__ " + umbrella2.ToString() + thunder_cloud_rain.ToString() + cloud_rain.ToString() + string.Format(@"

First to use '**{0}umbrella**' starts the Storm and earns {1} points! 10 minute countdown starting now!", await GetServerPrefix(serverId), levelOneReward)));

			StartStormCountdown(channelId);
		}

		public async Task EndStorm(ulong channelId)
		{
			OngoingStormsLevel.Remove(channelId);
			OngoingStormsWinningNumber.Remove(channelId);

			purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(sun_with_face.ToString() + sun_with_face.ToString() + sun_with_face.ToString() + " __**STORM OVER**__ " + sun_with_face.ToString() + sun_with_face.ToString() + sun_with_face.ToString()));

			// wait 1 minute
			await Task.Delay(60 * 1000);

			// delete all messages added to purge collection
			await ((ITextChannel)_client.GetChannel(channelId)).DeleteMessagesAsync(purgeCollection);
		}

		public async Task StartStormCountdown(ulong channelId)
		{
			int actualLevel;

			// end the storm in 10 minutes

			// wait 5 minutes
			await Task.Delay(300 * 1000);

			// announce 5 minutes left if still ongoing
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
				purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + " __**5 MINUTES REMAINING!**__ " + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString()));

			// wait 4 minutes
			await Task.Delay(240 * 1000);

			// announce 1 minute left if still ongoing
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
				purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + " __**1 MINUTE REMAINING!**__ " + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString()));

			// wait 1 minute
			await Task.Delay(60 * 1000);

			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
				await EndStorm(channelId);
		}

		public async Task TryToUpdateOngoingStorm(ulong serverId, ulong discordId, ulong channelId, int inputLevel, int? guess = null, int? bet = null)
		{
			int actualLevel;

			// if there is an ongoing storm
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
			{
				// if the ongoing storm is on the correct step for this command, give the user points and update the storm level
				if (actualLevel == inputLevel)
				{
					StormPlayerDataEntity playerData = await GetStormPlayerDataEntity(serverId, discordId);

					if (actualLevel == 1)
					{
						// give user points for level 1
						playerData.Wallet += levelOneReward;
						await _db.SaveChangesAsync();

						purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you put up your umbrella first and earned {levelOneReward} points!" + string.Format(@"

__**First to guess the winning number correctly between 1 and 200 earns points!**__
Use '**{0}guess [number]**' to make a guess with a winning reward of {1} points!
Use '**{0}bet [points] [number]**' to make a guess. If you win, you earn the amount of points bet within your wallet. If you lose, you lose those points.
Use '**{0}wallet**' to show how many points you have in your wallet!
Use '**{0}wallets**' to show how many points everyone has!", await GetServerPrefix(serverId), levelTwoReward)));

						// update storm to level 2
						OngoingStormsLevel[channelId] = 2;
					}
					else if (actualLevel == 2)
					{
						if (guess == OngoingStormsWinningNumber[channelId])
						{
							// give user points for level 2 (default levelTwoReward, or points bet)
							if (bet != null && bet.Value > levelTwoReward)
								levelTwoReward = bet.Value;

							playerData.Wallet += levelTwoReward;
							await _db.SaveChangesAsync();

							purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you guessed correctly and earned {levelTwoReward} points!"));

							// end storm at level 3
							OngoingStormsLevel[channelId] = 3;
							await EndStorm(channelId);
						}
						else
						{
							string message = $"<@!{discordId}>, you guessed incorrectly";

							if (bet != null)
							{
								message += $" and lost {bet.Value} points.\n";

								// take points from user if they bet
								int newWallet = playerData.Wallet - bet.Value;
								if (newWallet < 0)
									playerData.Wallet = 0;
								else
									playerData.Wallet = newWallet;

								await _db.SaveChangesAsync();
							}
							else
							{
								message += ".\n";
							}

							message += "The winning number is ";

							if (guess < OngoingStormsWinningNumber[channelId])
							{
								message += "greater than ";
							}
							else
							{
								message += "less than ";
							}

							message += guess + ".";

							purgeCollection.Add(await((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(message));
						}
					}
				}
			}
		}

		#region QUERIES
		public async Task<StormPlayerDataEntity> AddPlayerToDbTableIfNotExist(ulong serverID, ulong discordID)
		{
			StormPlayerDataEntity playerData = await GetStormPlayerDataEntity(serverID, discordID);

			if (playerData == null)
			{
				StormPlayerDataEntity newPlayerData = new StormPlayerDataEntity()
				{
					ServerID = serverID,
					DiscordID = discordID,
					Wallet = 0
				};

				_db.StormPlayerData.Add(newPlayerData);
				await _db.SaveChangesAsync();

				return newPlayerData;
			}

			return playerData;
		}

		public async Task<StormPlayerDataEntity> GetStormPlayerDataEntity(ulong serverID, ulong discordID)
		{
			return await _db.StormPlayerData
				.AsQueryable()
				.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
				.SingleOrDefaultAsync();
		}

		public async Task<List<StormPlayerDataEntity>> GetAllStormPlayerDataEntities(ulong serverID)
		{
			return await _db.StormPlayerData
				.AsQueryable()
				.Where(player => player.ServerID == serverID)
				.AsAsyncEnumerable()
				.ToListAsync();
		}

		public async Task<bool> GetServerToggleStorms(SocketCommandContext context)
		{
			if (!context.IsPrivate)
			{
				bool flag = await _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == context.Guild.Id)
				.Select(s => s.ToggleStorms)
				.SingleAsync();

				if (!flag)
					Console.WriteLine($"Command will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

				return flag;
			}
			else
				return true;
		}

		public async Task<bool> GetServerAllowServerPermissionStorms(SocketCommandContext context)
		{
			if (!context.IsPrivate)
			{
				bool flag = await _db.Servers
				.AsQueryable()
				.Where(s => s.ServerID == context.Guild.Id)
				.Select(s => s.AllowServerPermissionStorms)
				.SingleAsync();

				if (!flag)
					Console.WriteLine($"Command will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

				return flag;
			}
			else
				return true;
		}
		#endregion
	}
}
