using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StormBot.Database;
using StormBot.Database.Entities;
using StormBot.Models.MarketModels;

namespace StormBot.Services
{
	public class MarketService : BaseService
	{
		public static DiscordSocketClient _client;

		private static List<PendingTransactionModel> PendingTransactions;

		public MarketService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();

			Name = "Market Service";
			IsServiceRunning = false;

			PendingTransactions = new List<PendingTransactionModel>();
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

					if (IsServiceRunning && server.AllowServerPermissionMarket && server.ToggleMarket)
					{
						message += "_**[    MARKET ONLINE.    ]**_\n";
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

					if (IsServiceRunning && server.AllowServerPermissionMarket && server.ToggleMarket)
					{
						message += "_**[    MARKET OFFLINE.    ]**_\n";
					}

					if (server.StormsNotificationChannelID != 0 && message != "")
						await ((IMessageChannel)_client.GetChannel(server.StormsNotificationChannelID)).SendMessageAsync(message);
				}

				IsServiceRunning = false;
			}
		}

		public static bool OpenPendingTransaction(ulong serverID, ulong buyerID, ulong ownerID, string itemName)
		{
			if (!PendingTransactions.Exists(transaction => transaction.ServerID == serverID && transaction.BuyerID == buyerID && transaction.OwnerID == ownerID && transaction.ItemName == itemName))
			{
				PendingTransactionModel transaction = new PendingTransactionModel()
				{
					ServerID = serverID,
					BuyerID = buyerID,
					OwnerID = ownerID,
					ItemName = itemName,
					Status = -1
				};

				PendingTransactions.Add(transaction);

				return true;
			}
			else
				return false;
		}

		public static int? GetTransactionStatus(ulong serverID, ulong buyerID, ulong ownerID, string itemName)
		{
			if (PendingTransactions.Exists(transaction => transaction.ServerID == serverID && transaction.BuyerID == buyerID && transaction.OwnerID == ownerID && transaction.ItemName == itemName))
				return PendingTransactions.Find(transaction => transaction.ServerID == serverID && transaction.BuyerID == buyerID && transaction.OwnerID == ownerID && transaction.ItemName == itemName).Status;
			else
				return null;
		}

		public static void UpdateTransactionStatusSold(ulong serverID, ulong buyerID, ulong ownerID, string itemName)
		{
			PendingTransactions.Find(transaction => transaction.ServerID == serverID && transaction.BuyerID == buyerID && transaction.OwnerID == ownerID && transaction.ItemName == itemName).Status = 1;
		}

		public static int ClosePendingTransaction(ulong serverID, ulong buyerID, ulong ownerID, string itemName)
		{
			return PendingTransactions.RemoveAll(transaction => transaction.ServerID == serverID && transaction.BuyerID == buyerID && transaction.OwnerID == ownerID && transaction.ItemName == itemName);
		}



		#region QUERIES
		public static void AddPlayerToDbTableIfNotExist(ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				MarketPlayerDataEntity playerData = _db.MarketPlayerData
					.AsQueryable()
					.Where(player => player.DiscordID == discordID)
					.SingleOrDefault();

				if (playerData == null)
				{
					MarketPlayerDataEntity newPlayerData = new MarketPlayerDataEntity()
					{
						DiscordID = discordID,
						MarketItemsJSON = "{\"items\":[]}"
					};

					_db.MarketPlayerData.Add(newPlayerData);
					_db.SaveChanges();
				}
			}
		}

		public static bool AddPlayerMarketItem(ulong discordID, MarketItemModel item)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				try
				{
					MarketPlayerDataEntity entity = _db.MarketPlayerData
						.AsQueryable()
						.Where(player => player.DiscordID == discordID)
						.SingleOrDefault();

					MarketItemsModel marketItemsModel = JsonConvert.DeserializeObject<MarketItemsModel>(entity.MarketItemsJSON);
					marketItemsModel.Items.Add(item);
					entity.MarketItemsJSON = JsonConvert.SerializeObject(marketItemsModel);

					_db.SaveChanges();

					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		public static int RemovePlayerMarketItem(ulong discordID, string itemName)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				MarketPlayerDataEntity entity = _db.MarketPlayerData
					.AsQueryable()
					.Where(player => player.DiscordID == discordID)
					.SingleOrDefault();

				MarketItemsModel marketItemsModel = JsonConvert.DeserializeObject<MarketItemsModel>(entity.MarketItemsJSON);
				int removed = marketItemsModel.Items.RemoveAll(item => item.Name == itemName);
				entity.MarketItemsJSON = JsonConvert.SerializeObject(marketItemsModel);

				_db.SaveChanges();

				return removed;
			}
		}

		public static MarketItemsModel GetAllPlayerMarketItems(ulong discordID)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				string marketItemsJSON = _db.MarketPlayerData
					.AsQueryable()
					.Where(player => player.DiscordID == discordID)
					.Select(player => player.MarketItemsJSON)
					.SingleOrDefault();

				if (marketItemsJSON != null)
					return JsonConvert.DeserializeObject<MarketItemsModel>(marketItemsJSON);
				else
					return null;
			}
		}

		public static MarketItemModel GetPlayerMarketItem(ulong discordID, string itemName)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				string marketItemsJSON = _db.MarketPlayerData
					.AsQueryable()
					.Where(player => player.DiscordID == discordID)
					.Select(player => player.MarketItemsJSON)
					.SingleOrDefault();

				if (marketItemsJSON != null)
				{
					MarketItemsModel marketItemsModel = JsonConvert.DeserializeObject<MarketItemsModel>(marketItemsJSON);
					return marketItemsModel.Items.Find(item => item.Name == itemName);
				}
				else
					return null;
			}
		}

		public static bool UpdatePlayerMarketItemName(ulong discordID, string itemName, string newName)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				try
				{
					MarketPlayerDataEntity entity = _db.MarketPlayerData
						.AsQueryable()
						.Where(player => player.DiscordID == discordID)
						.SingleOrDefault();

					MarketItemsModel marketItemsModel = JsonConvert.DeserializeObject<MarketItemsModel>(entity.MarketItemsJSON);
					marketItemsModel.Items.Find(item => item.Name == itemName).Name = newName;
					entity.MarketItemsJSON = JsonConvert.SerializeObject(marketItemsModel);

					_db.SaveChanges();

					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		public static bool GetServerToggleMarket(SocketCommandContext context)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				if (!context.IsPrivate)
				{
					bool flag = _db.Servers
						.AsQueryable()
						.Where(s => s.ServerID == context.Guild.Id)
						.Select(s => s.ToggleMarket)
						.Single();

					if (!flag)
						Console.WriteLine($"Market commands will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

					return flag;
				}
				else
					return true;
			}
		}

		public static bool GetServerAllowServerPermissionMarket(SocketCommandContext context)
		{
			using (StormBotContext _db = new StormBotContext())
			{
				if (!context.IsPrivate)
				{
					bool flag = _db.Servers
						.AsQueryable()
						.Where(s => s.ServerID == context.Guild.Id)
						.Select(s => s.AllowServerPermissionMarket)
						.Single();

					if (!flag)
						Console.WriteLine($"Market commands will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

					return flag;
				}
				else
					return true;
			}
		}
		#endregion
	}
}
