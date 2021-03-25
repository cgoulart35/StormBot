using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class StormsService : BaseService
	{
		public static DiscordSocketClient _client;

		public static Dictionary<ulong, int> OngoingStormsLevel;
		private static Dictionary<ulong, int> OngoingStormsWinningNumber;
		private static Dictionary<ulong, Dictionary<ulong, int>> OngoingStormsUserGuessCount;
		private static Dictionary<ulong, List<ulong>> OngoingStormsUsersWaitingForStealTimeLimit;

		public static Dictionary<ulong, List<IUserMessage>> PurgeCollection;

		private static Random random;

		public static Emoji cloud_rain;
		public static Emoji thunder_cloud_rain;
		public static Emoji umbrella2;
		public static Emoji white_sun_rain_cloud;
		public static Emoji sun_with_face;
		public static Emoji rotating_light;

		public static double levelOneReward = 10;
		public static double levelTwoReward = 50;
		public static double resetBalance = 10;
		public static double resetMark = 25000;
		public static double disasterMark = 150;
		public static double insuranceCost = 500;
		public static double stealAmount = 5;
		public static int stealTimeLimitInSeconds = 10;

		public StormsService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();

			Name = "Storm Service";
			IsServiceRunning = false;

			OngoingStormsLevel = new Dictionary<ulong, int>();
			OngoingStormsWinningNumber = new Dictionary<ulong, int>();
			OngoingStormsUserGuessCount = new Dictionary<ulong, Dictionary<ulong, int>>();
			OngoingStormsUsersWaitingForStealTimeLimit = new Dictionary<ulong, List<ulong>>();

			PurgeCollection = new Dictionary<ulong, List<IUserMessage>>();
			List<ServersEntity> servers = GetAllServerEntities();
			foreach (ServersEntity server in servers)
			{
				if (server.AllowServerPermissionStorms && server.ToggleStorms && server.StormsNotificationChannelID != 0)
					PurgeCollection.Add(server.StormsNotificationChannelID, new List<IUserMessage>());
			}

			random = new Random();

			cloud_rain = new Emoji("🌧️");
			thunder_cloud_rain = new Emoji("⛈️");
			umbrella2 = new Emoji("☂️");
			white_sun_rain_cloud = new Emoji("🌦️");
			sun_with_face = new Emoji("🌞");
			rotating_light = new Emoji("🚨");
		}

		public override async Task StartService()
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

				List<ServersEntity> servers = GetAllServerEntities();
				foreach (ServersEntity server in servers)
				{
					string message = "";

					if (IsServiceRunning && server.AllowServerPermissionStorms && server.ToggleStorms)
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

			if (IsServiceRunning)
			{
				Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

				List<ServersEntity> servers = GetAllServerEntities();
				foreach (ServersEntity server in servers)
				{
					string message = "";

					if (IsServiceRunning && server.AllowServerPermissionStorms && server.ToggleStorms)
					{
						message += "_**[    STORMS OFFLINE.    ]**_\n";
					}

					if (server.StormsNotificationChannelID != 0 && message != "")
						await ((IMessageChannel)_client.GetChannel(server.StormsNotificationChannelID)).SendMessageAsync(message);
				}

				IsServiceRunning = false;
			}
		}

		public async Task HandleIncomingStorm(object sender, ulong serverId, string serverName, ulong channelId)
		{
			string logStamp = GetLogStamp();

			int randomNumber = random.Next(1, 201);
			Console.WriteLine(logStamp + $"				The winning number for the ongoing Storm in {serverName} is {randomNumber}.");

			OngoingStormsLevel.Add(channelId, 1);
			OngoingStormsWinningNumber.Add(channelId, randomNumber);

			Dictionary<ulong, int> UserGuessCountsInServer = new Dictionary<ulong, int>();
			OngoingStormsUserGuessCount.Add(channelId, UserGuessCountsInServer);

			List<ulong> UsersWaitingInServerForSteal = new List<ulong>();
			OngoingStormsUsersWaitingForStealTimeLimit.Add(channelId, UsersWaitingInServerForSteal);

			IUserMessage message = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(cloud_rain.ToString() + thunder_cloud_rain.ToString() + umbrella2.ToString() + " __**STORM INCOMING**__ " + umbrella2.ToString() + thunder_cloud_rain.ToString() + cloud_rain.ToString() + string.Format(@"

First to use '**{0}umbrella**' starts the Storm and earns {1} points! 10 minute countdown starting now!", GetServerPrefix(serverId), levelOneReward));

			if (PurgeCollection.ContainsKey(channelId))
				PurgeCollection[channelId].Add(message);

				StartStormCountdown(channelId);
		}

		public static async Task EndStorm(ulong channelId)
		{
			bool wasRemoved = OngoingStormsLevel.Remove(channelId);
			OngoingStormsWinningNumber.Remove(channelId);
			OngoingStormsUserGuessCount.Remove(channelId);
			OngoingStormsUsersWaitingForStealTimeLimit.Remove(channelId);

			if (wasRemoved)
			{
				IUserMessage message = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(sun_with_face.ToString() + sun_with_face.ToString() + sun_with_face.ToString() + " __**STORM OVER**__ " + sun_with_face.ToString() + sun_with_face.ToString() + sun_with_face.ToString());
				if (PurgeCollection.ContainsKey(channelId))
					PurgeCollection[channelId].Add(message);

				// wait 1 minute
				await Task.Delay(60 * 1000);

				// delete all messages added to purge collection
				if (PurgeCollection.ContainsKey(channelId))
					await ((ITextChannel)_client.GetChannel(channelId)).DeleteMessagesAsync(PurgeCollection[channelId]);
			}
		}

		public static async Task StartStormCountdown(ulong channelId)
		{
			int actualLevel;

			// end the storm in 10 minutes

			// wait 5 minutes
			await Task.Delay(300 * 1000);

			// announce 5 minutes left if still ongoing
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
			{
				IUserMessage userMessage = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + " __**5 MINUTES REMAINING!**__ " + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString());
				if (PurgeCollection.ContainsKey(channelId))
					PurgeCollection[channelId].Add(userMessage);
			}

			// wait 4 minutes
			await Task.Delay(240 * 1000);

			// announce 1 minute left if still ongoing
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
			{
				IUserMessage userMessage = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + " __**1 MINUTE REMAINING!**__ " + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString() + white_sun_rain_cloud.ToString());
				if (PurgeCollection.ContainsKey(channelId))
					PurgeCollection[channelId].Add(userMessage);
			}
			// wait 1 minute
			await Task.Delay(60 * 1000);

			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
				await EndStorm(channelId);
		}

		public static async Task StartUsersStealTimeLimitCountdown(ulong channelId, ulong discordId)
		{
			// wait x seconds before removal from waitlist
			await Task.Delay(stealTimeLimitInSeconds * 1000);

			OngoingStormsUsersWaitingForStealTimeLimit[channelId].Remove(discordId);
		}

		public static async Task TryToUpdateOngoingStorm(SocketGuild guild, ulong serverId, ulong discordId, ulong channelId, int inputLevel, int? guess = null, double? bet = null)
		{
			int actualLevel;

			// if there is an ongoing storm
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel))
			{
				// if the ongoing storm is on the correct step for this command, give the user points and update the storm level
				if (actualLevel == inputLevel)
				{
					bool hadDisasterMark = GetPlayerWallet(serverId, discordId) >= disasterMark;

					if (actualLevel == 1)
					{
						// give user points for level 1
						AddPointsToPlayersWallet(serverId, discordId, levelOneReward);

						IUserMessage userMessage1 = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you put up your umbrella first and earned {levelOneReward} points!" + string.Format(@"

__**First to guess the winning number correctly between 1 and 200 earns points!**__
Use '**{0}guess [number]**' to make a guess with a winning reward of {1} points!
Use '**{0}bet [points] [number]**' to make a guess. If you win, you earn the amount of points bet within your wallet. If you lose, you lose those points.
Use '**{0}steal**' to steal {2} points from the player with the most points.

Use '**{0}buy insurance**' to buy insurance for {3} points to protect your wallet from disasters.
Use '**{0}wallet**' to show how many points you have in your wallet!
Use '**{0}wallets**' to show how many points everyone has!
Use '**{0}resets**' to show how many resets everyone has.

Points earned are multiplied if you guess within 4 guesses!
When anyone reaches {4} points, a disaster will occur for a random player. Their wallet will be reset to {5} points if they are not insured.
All wallets are reset to {5} points once someone reaches {6} points.", GetServerPrefix(serverId), levelTwoReward, stealAmount, insuranceCost, disasterMark, resetBalance, resetMark));

						if (PurgeCollection.ContainsKey(channelId))
							PurgeCollection[channelId].Add(userMessage1);

						// update storm to level 2
						OngoingStormsLevel[channelId] = 2;
					}
					else if (actualLevel == 2)
					{
						// if user has guessed, get count; otherwise, set count to 0
						int guessCount;
						if (!OngoingStormsUserGuessCount[channelId].TryGetValue(discordId, out guessCount))
							 guessCount = 0;

						// store user's guess count as 1 if it's their first guess
						if (guessCount == 0)
						{
							guessCount = 1;
							OngoingStormsUserGuessCount[channelId].Add(discordId, guessCount);
						}
						// if it's not the users first guess, increment the count
						else
						{
							OngoingStormsUserGuessCount[channelId][discordId] += 1;
							guessCount = OngoingStormsUserGuessCount[channelId][discordId];
						}

						double multiplier = 1;
						if (guessCount == 1)
							multiplier = 10;
						else if (guessCount == 2)
							multiplier = 5;
						else if (guessCount == 3)
							multiplier = 2.5;
						else if (guessCount == 4)
							multiplier = 1.25;

						if (guess == OngoingStormsWinningNumber[channelId])
						{
							// give user points for level 2 (default levelTwoReward, or points bet)
							double reward = levelTwoReward;
							if (bet != null && bet.Value > levelTwoReward)
								reward = bet.Value;

							AddPointsToPlayersWallet(serverId, discordId, reward * multiplier);

							string multiplierStr = "";
							if (multiplier > 1)
								multiplierStr = $" ( **{guessCount} GUESSES:** {reward} points x{multiplier} multiplier )";

							IUserMessage userMessage2 = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you guessed correctly and earned {reward * multiplier} points!" + multiplierStr);
							if (PurgeCollection.ContainsKey(channelId))
								PurgeCollection[channelId].Add(userMessage2);

							// end storm at level 3
							OngoingStormsLevel[channelId] = 3;
							EndStorm(channelId);
						}
						else
						{
							string message = $"<@!{discordId}>, you guessed incorrectly";

							if (bet != null)
							{
								message += $" and lost {bet.Value} points.\n";

								// take points from user if they bet
								SubtractPointsFromPlayersWallet(serverId, discordId, bet.Value);
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

							IUserMessage userMessage3 = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(message);
							if (PurgeCollection.ContainsKey(channelId))
								PurgeCollection[channelId].Add(userMessage3);
						}
					}

					await CheckForReset(guild, serverId, discordId, channelId);
					await CheckForDisaster(serverId, discordId, channelId, hadDisasterMark);
				}
			}
		}

		public static async Task TryToSteal(SocketGuild guild, ulong serverId, ulong discordId, ulong channelId)
		{
			int actualLevel;

			// make sure that there is an ongoing storm on level two
			if (OngoingStormsLevel.TryGetValue(channelId, out actualLevel) && actualLevel == 2)
			{
				// make sure user has not stolen in the last ten seconds
				if (!OngoingStormsUsersWaitingForStealTimeLimit[channelId].Contains(discordId))
				{
					// add user to list of users waiting and trigger removal after set time in seconds
					OngoingStormsUsersWaitingForStealTimeLimit[channelId].Add(discordId);
					StartUsersStealTimeLimitCountdown(channelId, discordId);

					AddPlayerToDbTableIfNotExist(serverId, discordId);

					bool hadDisasterMark = GetPlayerWallet(serverId, discordId) >= disasterMark;

					// get the top players to steal from (exclude the criminal/person stealing) & make sure there is atleast one player with a score above zero
					List<StormPlayerDataEntity> topPlayers = GetTopPlayersWithMostPoints(serverId, discordId);

					// do not let users steal from just themselves (when count is zero)
					if (topPlayers != null && topPlayers.Any())
					{
						// set top players' wallets and criminal's wallet
						foreach (StormPlayerDataEntity topPlayer in topPlayers)
						{
							double oldWalletTopPlayer = topPlayer.Wallet;
							double newWalletTopPlayer = SubtractPointsFromPlayersWallet(topPlayer.ServerID, topPlayer.DiscordID, stealAmount / topPlayers.Count);

							double diff;
							if (newWalletTopPlayer == 0)
								diff = oldWalletTopPlayer;
							else
								diff = stealAmount / topPlayers.Count;

							AddPointsToPlayersWallet(serverId, discordId, diff);

							IUserMessage message = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you stole {diff} points from <@!{topPlayer.DiscordID}>!");
							if (PurgeCollection.ContainsKey(channelId))
								PurgeCollection[channelId].Add(message);

							await CheckForReset(guild, serverId, discordId, channelId);
							await CheckForDisaster(serverId, discordId, channelId, hadDisasterMark);
						}
					}
				}
				else
				{
					IUserMessage message = await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, please wait {stealTimeLimitInSeconds} seconds before stealing again.");
					if (PurgeCollection.ContainsKey(channelId))
						PurgeCollection[channelId].Add(message);

				}
			}
		}

		private static async Task CheckForReset(SocketGuild guild, ulong serverId, ulong discordId, ulong channelId)
		{
			// increment players reset count, set everyones wallets back to base amount, and give appropriate roles
			if (GetPlayerWallet(serverId, discordId) >= resetMark)
			{
				// end the ongoing storm if there is a reset
				EndStorm(channelId);

				// give everyone the base wallet amount and no insurance, and increment player's reset count
				PerformReset(serverId, discordId);

				ulong mostRecentRoleID = GetStormsMostRecentResetRoleID(serverId);
				ulong mostResetsRoleID = GetStormsMostResetsRoleID(serverId);

				// unassign both roles from everyone
				await UnassignRoleFromAllMembers(mostResetsRoleID, guild);
				await UnassignRoleFromAllMembers(mostRecentRoleID, guild);

				// assign most recent reset role to the resetting player
				List<ulong> resettingPlayer = new List<ulong>();
				resettingPlayer.Add(discordId);
				await GiveUsersRole(mostRecentRoleID, resettingPlayer, guild);

				// assign the most resets role to the player(s) with the most resets
				List<ulong> topPlayersDiscordIDs = GetTopPlayersWithMostResets(serverId);
				await GiveUsersRole(mostResetsRoleID, topPlayersDiscordIDs, guild);

				string topPlayersStr = "";
				foreach (ulong DiscordID in topPlayersDiscordIDs)
				{
					topPlayersStr += string.Format(@"<@!{0}>, ", DiscordID);
				}

				// display reset message and post role announcements; this message is rare and therefore is not purged with other messages
				await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync(rotating_light.ToString() + rotating_light.ToString() + rotating_light.ToString() + " __**RESET TRIGGERED**__ " + rotating_light.ToString() + rotating_light.ToString() + rotating_light.ToString() + string.Format(@"

Congratulations <@!{0}>, you passed {1} points and triggered a reset! You have been given the <@&{2}> role. Everyone now has {3} points in their wallet and no insurance.

{4}you currently have the <@&{5}> role.", discordId, resetMark, mostRecentRoleID, resetBalance, topPlayersStr, mostResetsRoleID));
			}
		}

		private static async Task CheckForDisaster(ulong serverId, ulong discordId, ulong channelId, bool hadDisasterMark)
		{
			if (GetPlayerWallet(serverId, discordId) >= resetMark && !hadDisasterMark)
			{
				// reset random player's wallet if they are uninsured
				ulong randomDiscordID = GetRandomPlayerDiscordID(serverId);

				string theyYouStr = "";
				string theirYour = "";
				string onPersonAffected = "";
				if (randomDiscordID == discordId)
				{
					theyYouStr = " You";
					theirYour = " your";
					onPersonAffected = " on yourself";
				}
				else
				{
					theyYouStr = " They";
					theirYour = " their";
					onPersonAffected = $" for <@!{randomDiscordID}>";
				}

				string insuredOrNotStr = "";
				if (!GetPlayerInsurance(serverId, randomDiscordID))
				{
					double previousBalance = GetPlayerWallet(serverId, randomDiscordID);
					PerformDisaster(serverId, randomDiscordID);
					insuredOrNotStr = theyYouStr + " were not insured and" + theirYour + " wallet has been reset from " + previousBalance + " points to " + GetPlayerWallet(serverId, randomDiscordID) + " points!";
				}
				else
					insuredOrNotStr = " However," + theyYouStr + " were insured and not affected.";

				await ((IMessageChannel)_client.GetChannel(channelId)).SendMessageAsync($"<@!{discordId}>, you caused a disaster" + onPersonAffected + $" since you passed {disasterMark} points!" + insuredOrNotStr);
			}
		}

		#region QUERIES
		public static void AddPlayerToDbTableIfNotExist(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				if (playerData == null)
				{
					StormPlayerDataEntity newPlayerData = new StormPlayerDataEntity()
					{
						ServerID = serverID,
						DiscordID = discordID,
						Wallet = 0,
						ResetCount = 0,
						HasInsurance = false
					};

					_db.StormPlayerData.Add(newPlayerData);
					_db.SaveChanges();
				}
			}
		}

		public static void AddInsuranceForPlayer(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				playerData.Wallet -= insuranceCost;
				playerData.HasInsurance = true;
				_db.SaveChanges();
			}
		}

		public static double AddPointsToPlayersWallet(ulong serverID, ulong discordID, double pointsToAdd)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				playerData.Wallet += pointsToAdd;
				_db.SaveChanges();

				return playerData.Wallet;
			}
		}

		public static double SubtractPointsFromPlayersWallet(ulong serverID, ulong discordID, double pointsToSubtract)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				double newWallet = playerData.Wallet - pointsToSubtract;

				if (newWallet < 0)
					playerData.Wallet = 0;
				else
					playerData.Wallet = newWallet;

				_db.SaveChanges();

				return playerData.Wallet;
			}
		}

		private static void PerformReset(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				List<StormPlayerDataEntity> allPlayerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID)
					.AsEnumerable()
					.ToList();

				foreach (StormPlayerDataEntity player in allPlayerData)
				{
					player.Wallet = resetBalance;
					player.HasInsurance = false;
				}

				StormPlayerDataEntity playerData = allPlayerData.Find(playerData => playerData.DiscordID == discordID);
				playerData.ResetCount++;

				_db.SaveChanges();
			}
		}

		private static void PerformDisaster(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				if (playerData.Wallet <= resetBalance)
					playerData.Wallet = 0;
				else
					playerData.Wallet = resetBalance;

				_db.SaveChanges();
			}
		}

		public static List<StormPlayerDataEntity> GetAllStormPlayerDataEntities(ulong serverID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID)
					.AsEnumerable()
					.ToList();
			}
		}

		private static ulong GetRandomPlayerDiscordID(ulong serverID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				List<StormPlayerDataEntity> allPlayerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID)
					.AsEnumerable()
					.ToList();

				int randomIndex = random.Next(0, allPlayerData.Count);

				return allPlayerData[randomIndex].DiscordID;
			}
		}

		private static List<StormPlayerDataEntity> GetTopPlayersWithMostPoints(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				List<StormPlayerDataEntity> allPlayerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID)
					.AsEnumerable()
					.ToList();

				double? topScore = allPlayerData.OrderByDescending(player => player.Wallet).Select(player => player.Wallet).FirstOrDefault();

				if (topScore != null && topScore != 0)
					return allPlayerData.Where(player => player.Wallet == topScore && player.DiscordID != discordID).ToList();
				else
					return null;
			}
		}

		private static List<ulong> GetTopPlayersWithMostResets(ulong serverID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				List<StormPlayerDataEntity> allPlayerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID)
					.AsEnumerable()
					.ToList();

				int topScore = allPlayerData.OrderByDescending(player => player.ResetCount).First().ResetCount;

				return allPlayerData.Where(player => player.ResetCount == topScore).Select(player => player.DiscordID).ToList();
			}
		}

		public static double GetPlayerWallet(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				return playerData.Wallet;
			}
		}

		public static bool GetPlayerInsurance(ulong serverID, ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				StormPlayerDataEntity playerData = _db.StormPlayerData
					.AsQueryable()
					.Where(player => player.ServerID == serverID && player.DiscordID == discordID)
					.SingleOrDefault();

				return playerData.HasInsurance;
			}
		}

		public static IMessageChannel GetServerStormsNotificationChannel(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				var channelId = _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Select(s => s.StormsNotificationChannelID)
					.SingleOrDefault();

				if (channelId != 0)
					return _client.GetChannel(channelId) as IMessageChannel;
				else
					return null;
			}
		}

		public static bool GetServerToggleStorms(SocketCommandContext context)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				if (!context.IsPrivate)
				{
					bool flag = _db.Servers
						.AsQueryable()
						.Where(s => s.ServerID == context.Guild.Id)
						.Select(s => s.ToggleStorms)
						.Single();

					if (!flag)
						Console.WriteLine($"Storms commands will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

					return flag;
				}
				else
					return true;
			}
		}

		public static bool GetServerAllowServerPermissionStorms(SocketCommandContext context)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				if (!context.IsPrivate)
				{
					bool flag = _db.Servers
						.AsQueryable()
						.Where(s => s.ServerID == context.Guild.Id)
						.Select(s => s.AllowServerPermissionStorms)
						.Single();

					if (!flag)
						Console.WriteLine($"Storms commands will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

					return flag;
				}
				else
					return true;
			}
		}

		public static ulong GetStormsMostResetsRoleID(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Select(s => s.StormsMostResetsRoleID)
					.SingleOrDefault();
			}
		}

		public static ulong GetStormsMostRecentResetRoleID(ulong serverId)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				return _db.Servers
					.AsQueryable()
					.Where(s => s.ServerID == serverId)
					.Select(s => s.StormsMostRecentResetRoleID)
					.SingleOrDefault();
			}
		}
		#endregion
	}
}
