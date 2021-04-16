using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestSharp;
using StormBot.Models.CallOfDutyModels;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class CallOfDutyService : BaseService
    {
        public static DiscordSocketClient _client;

        public BaseService BlackOpsColdWarComponent { get; set; }

        public BaseService ModernWarfareComponent { get; set; }

        public BaseService WarzoneComponent { get; set; }

        public CallOfDutyService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();

            Name = "Call Of Duty Service";
            IsServiceRunning = false;

            BlackOpsColdWarComponent = new BaseService();
            BlackOpsColdWarComponent.Name = "Black Ops Cold War Service";
            BlackOpsColdWarComponent.IsServiceRunning = false;

            ModernWarfareComponent = new BaseService();
            ModernWarfareComponent.Name = "Modern Warfare Service";
            ModernWarfareComponent.IsServiceRunning = false;

            WarzoneComponent = new BaseService();
            WarzoneComponent.Name = "Warzone Service";
            WarzoneComponent.IsServiceRunning = false;
        }

        public override async Task StartService()
        {
            string logStamp = GetLogStamp();

            if (!DoStart || !(BlackOpsColdWarComponent.DoStart || ModernWarfareComponent.DoStart || WarzoneComponent.DoStart))
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

                await BlackOpsColdWarComponent.StartService();
                await ModernWarfareComponent.StartService();
                await WarzoneComponent.StartService();

                List<ServersEntity> servers = GetAllServerEntities();
                foreach (ServersEntity server in servers)
                {
                    string message = "";

                    if (BlackOpsColdWarComponent.IsServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking)
                    {
                        message += "_**[    BLACK OPS COLD WAR TRACKING ONLINE.    ]**_\n";
                    }
                    if (ModernWarfareComponent.IsServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking)
                    {
                        message += "_**[    MODERN WARFARE TRACKING ONLINE.    ]**_\n";
                    }
                    if (WarzoneComponent.IsServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking)
                    {
                        message += "_**[    WARZONE TRACKING ONLINE.    ]**_";
                    }

                    if (server.CallOfDutyNotificationChannelID != 0 && message != "")
                        await ((IMessageChannel) _client.GetChannel(server.CallOfDutyNotificationChannelID)).SendMessageAsync(message);
                }
            }
        }

        public override async Task StopService()
        {
            string logStamp = GetLogStamp();

            if (IsServiceRunning)
            {
                Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

                IsServiceRunning = false;

                List<ServersEntity> servers = GetAllServerEntities();
                foreach (ServersEntity server in servers)
                {
                    string message = "";

                    if (BlackOpsColdWarComponent.IsServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking)
                    {
                        message += "_**[    BLACK OPS COLD WAR TRACKING OFFLINE.    ]**_\n";
                    }
                    if (ModernWarfareComponent.IsServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking)
                    {
                        message += "_**[    MODERN WARFARE TRACKING OFFLINE.    ]**_\n";
                    }
                    if (WarzoneComponent.IsServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking)
                    {
                        message += "_**[    WARZONE TRACKING OFFLINE.    ]**_";
                    }

                    if (server.CallOfDutyNotificationChannelID != 0 && message != "")
                        await ((IMessageChannel)_client.GetChannel(server.CallOfDutyNotificationChannelID)).SendMessageAsync(message);
                }

                await BlackOpsColdWarComponent.StopService();
                await ModernWarfareComponent.StopService();
                await WarzoneComponent.StopService();
            }
        }

		#region API
		public List<CallOfDutyPlayerModel> GetNewPlayerData(bool storeToDatabase, ulong serverId, string gameAbbrev, string modeAbbrev)
		{
			List<ulong> serverIds = new List<ulong>();
			serverIds.Add(serverId);

			// get all data of all players in the specified servers for the specified games
			List<CallOfDutyPlayerDataEntity> allStoredPlayersData = GetServersPlayerData(serverIds, gameAbbrev, modeAbbrev);

			// if no player data is returned, return null
			if (allStoredPlayersData.Count == 0)
			{
				string logStamp = GetLogStamp();
				Console.WriteLine(logStamp + "Cannot retrieve player data for zero players.".PadLeft(126 - logStamp.Length));
				return null;
			}

            RestClient client = new RestClient("https://profile.callofduty.com");
            client.Timeout = -1;
            client.UnsafeAuthenticatedConnectionSharing = true;

            CookieContainer cookieJar = new CookieContainer();
            client.CookieContainer = cookieJar;

            string XSRFTOKEN = "";

			// make initial request to get the XSRF-TOKEN
			InitializeAPI(client, ref XSRFTOKEN);

			// login to the API with credentials and XSRF-TOKEN to generate cookie tokens needed to retrieve player data
			LoginAPI(client, XSRFTOKEN);

			// retrieve updated data via Call of Duty API for all participating players with cookie tokens obtained from login
			List<CallOfDutyPlayerModel> allNewPlayersData = GetAllPlayersDataAPI(cookieJar, allStoredPlayersData);

            if (storeToDatabase)
                StoreNewPlayerDataToDatabase(allNewPlayersData, serverId, gameAbbrev, modeAbbrev);

            return allNewPlayersData;
		}

		private static void InitializeAPI(RestClient client, ref string XSRFTOKEN)
        {
            RestRequest request = new RestRequest("/null/login", Method.GET);
            client.Execute(request);

            XSRFTOKEN = client.CookieContainer.GetCookies(new Uri("https://profile.callofduty.com"))["XSRF-TOKEN"].Value;
        }

        private static void LoginAPI(RestClient client, string XSRFTOKEN)
        {
            string cookieHeader = client.CookieContainer.GetCookieHeader(new Uri("https://profile.callofduty.com"));

            RestRequest request = new RestRequest("do_login?new_SiteId=cod", Method.POST);
            request.AddHeader("Cookie", cookieHeader);
            request.AlwaysMultipartFormData = true;
            request.AddParameter("username", Program.configurationSettingsModel.ActivisionEmail);
            request.AddParameter("password", Program.configurationSettingsModel.ActivisionPassword);
            request.AddParameter("remember_me", "true");
            request.AddParameter("_csrf", XSRFTOKEN);
            client.Execute(request);
        }

        private List<CallOfDutyPlayerModel> GetAllPlayersDataAPI(CookieContainer cookieJar, List<CallOfDutyPlayerDataEntity> allStoredPlayersData)
        {
            // empty list of player data
            List<CallOfDutyPlayerModel> allNewPlayersData = new List<CallOfDutyPlayerModel>();

            // create CallOfDutyPlayerModel for each participating player from database with GetAPlayersData() & add each model to the allNewPlayerData list
            foreach (CallOfDutyPlayerDataEntity storedPlayerData in allStoredPlayersData)
            {
                // TODO: remove after debugging purposes fulfilled; rate limit issue?
                Console.WriteLine("Getting player data for: " + storedPlayerData.Username);

                CallOfDutyPlayerModel newPlayerData = GetAPlayersDataAPI(cookieJar, storedPlayerData);

                if (newPlayerData.Status == null || newPlayerData.Status != "success")
                {
                    string logStamp = GetLogStamp();
                    Console.WriteLine(logStamp + $"Error getting data for the Call of Duty account: ".PadLeft(100 - logStamp.Length) + storedPlayerData.Username);

                    // TODO: remove after debugging purposes fulfilled; rate limit issue?
                    Console.WriteLine(newPlayerData.Status);
                    Console.WriteLine(newPlayerData.DiscordID);
                    Console.WriteLine(newPlayerData.Data.Username);
                    Console.WriteLine(newPlayerData.Data.Lifetime.All.Properties.Kills);
                    Console.WriteLine(newPlayerData.Data.Lifetime.Mode.BattleRoyal.Properties.Wins);
                    Console.WriteLine(newPlayerData.Data.Weekly.All.Properties.Kills);

                    //return null;
                    continue;
                }

                // TODO: remove after debugging purposes fulfilled; rate limit issue?
                Console.WriteLine("Successfully got player data for: " + newPlayerData.Data.Username);

                newPlayerData.DiscordID = storedPlayerData.DiscordID;
                allNewPlayersData.Add(newPlayerData);
            }

            // create and return allPlayerDataModel with list
            // TODO: fix after debugging purposes fulfilled; rate limit issue?
            if (allNewPlayersData.Count != 0)
                return allNewPlayersData;
            else
                return null;
        }

        private static CallOfDutyPlayerModel GetAPlayersDataAPI(CookieContainer cookieJar, CallOfDutyPlayerDataEntity storedPlayerData)
        {
            string gameAbbrev = storedPlayerData.GameAbbrev;
            string modeAbbrev = storedPlayerData.ModeAbbrev;
            string platform = storedPlayerData.Platform;
            string username = storedPlayerData.Username;
            string tag = "";

            if (storedPlayerData.Tag != "" && storedPlayerData.Tag != null)
                tag = "%23" + storedPlayerData.Tag;

            RestClient client = new RestClient(string.Format(@"https://my.callofduty.com/api/papi-client/stats/cod/v1/title/{0}/platform/{1}/gamer/{2}{3}/profile/type/{4}", gameAbbrev, platform, username, tag, modeAbbrev));
            client.CookieContainer = cookieJar;
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            // convert API json response data into player data object
            return JsonConvert.DeserializeObject<CallOfDutyPlayerModel>(response.Content);
        }
        #endregion

        #region QUERIES
        private static void StoreNewPlayerDataToDatabase(List<CallOfDutyPlayerModel> allNewPlayersData, ulong serverId, string gameAbbrev, string modeAbbrev)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                List<CallOfDutyPlayerDataEntity> allStoredPlayersData = _db.CallOfDutyPlayerData
                    .AsQueryable()
                    .Where(player => serverId == player.ServerID && player.GameAbbrev == gameAbbrev && player.ModeAbbrev == modeAbbrev)
                    .AsEnumerable()
                    .ToList();

                foreach (CallOfDutyPlayerDataEntity storedPlayerData in allStoredPlayersData)
                {
                    CallOfDutyPlayerModel newPlayerData = allNewPlayersData.Find(newPlayerData => newPlayerData.DiscordID == storedPlayerData.DiscordID);

                    if (storedPlayerData.GameAbbrev == "mw" && storedPlayerData.ModeAbbrev == "mp")
                    {
                        storedPlayerData.TotalKills = newPlayerData.Data.Lifetime.All.Properties != null ? newPlayerData.Data.Lifetime.All.Properties.Kills : 0;
                        storedPlayerData.WeeklyKills = newPlayerData.Data.Weekly.All.Properties != null ? newPlayerData.Data.Weekly.All.Properties.Kills : 0;
                        storedPlayerData.Date = DateTime.Now;
                    }
                    else if (storedPlayerData.GameAbbrev == "mw" && storedPlayerData.ModeAbbrev == "wz")
                    {
                        storedPlayerData.TotalWins = newPlayerData.Data.Lifetime.Mode.BattleRoyal.Properties != null ? newPlayerData.Data.Lifetime.Mode.BattleRoyal.Properties.Wins : 0;
                        storedPlayerData.WeeklyKills = newPlayerData.Data.Weekly.All.Properties != null ? newPlayerData.Data.Weekly.All.Properties.Kills : 0;
                        storedPlayerData.Date = DateTime.Now;
                    }
                    else if (storedPlayerData.GameAbbrev == "cw" && storedPlayerData.ModeAbbrev == "mp")
                    {
                        storedPlayerData.TotalKills = newPlayerData.Data.Lifetime.All.Properties != null ? newPlayerData.Data.Lifetime.All.Properties.Kills : 0;
                        storedPlayerData.WeeklyKills = newPlayerData.Data.Weekly.All.Properties != null ? newPlayerData.Data.Weekly.All.Properties.Kills : 0;
                        storedPlayerData.Date = DateTime.Now;
                    }
                }

                _db.SaveChanges();
            }
        }

        public static List<CallOfDutyPlayerModel> GetServersPlayerDataAsPlayerModelList(ulong serverId, string gameAbbrev, string modeAbbrev)
        {
            List<ulong> serverIdList = new List<ulong>();
            serverIdList.Add(serverId);

            List<CallOfDutyPlayerDataEntity> entityData = GetServersPlayerData(serverIdList, gameAbbrev, modeAbbrev);

            List<CallOfDutyPlayerModel> modelData = new List<CallOfDutyPlayerModel>();

            foreach (CallOfDutyPlayerDataEntity entity in entityData)
            {
                CallOfDutyPlayerModel model = new CallOfDutyPlayerModel();
                if (gameAbbrev == "mw" && modeAbbrev == "mp")
                {
                    model.Data.Lifetime.All.Properties.Kills = entity.TotalKills;
                    model.Data.Weekly.All.Properties.Kills = entity.WeeklyKills;
                }
                else if (gameAbbrev == "mw" && modeAbbrev == "wz")
                {
                    model.Data.Lifetime.Mode.BattleRoyal.Properties.Wins = entity.TotalWins;
                    model.Data.Weekly.All.Properties.Kills = entity.WeeklyKills;
                }
                else if (gameAbbrev == "cw" && modeAbbrev == "mp")
                {
                    model.Data.Lifetime.All.Properties.Kills = entity.TotalKills;
                    model.Data.Weekly.All.Properties.Kills = entity.WeeklyKills;
                }

                model.DiscordID = entity.DiscordID;

                modelData.Add(model);
            }

            return modelData;
        }

        public static bool MissedLastDataFetch(ulong serverID, ulong discordID, string gameAbbrev, string modeAbbrev)
        {
            CallOfDutyPlayerDataEntity data = GetCallOfDutyPlayerDataEntity(serverID, discordID, gameAbbrev, modeAbbrev);

            DateTime lastDataFetchDay = DateTime.Now;

            // if today is Sunday, the last data fetch was this morning
            // if today is not Sunday, the last data fetch was the last Sunday
            while (lastDataFetchDay.DayOfWeek != DayOfWeek.Sunday)
                lastDataFetchDay = lastDataFetchDay.AddDays(-1);

            if (data.Date.Date == lastDataFetchDay.Date)
                return false;
            else
                return true;
        }

        public static void AddParticipantToDatabase(CallOfDutyPlayerDataEntity playerData)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                _db.CallOfDutyPlayerData.Add(playerData);
                _db.SaveChanges();
            }
        }

        public static bool RemoveParticipantFromDatabase(ulong serverID, ulong discordID, string gameAbbrev, string modeAbbrev)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                CallOfDutyPlayerDataEntity playerData = _db.CallOfDutyPlayerData
                    .AsQueryable()
                    .Where(player => player.ServerID == serverID && player.DiscordID == discordID && player.GameAbbrev == gameAbbrev && player.ModeAbbrev == modeAbbrev)
                    .SingleOrDefault();

                if (playerData != null)
                {
                    _db.CallOfDutyPlayerData.Remove(playerData);
                    _db.SaveChanges();

                    return true;
                }
                else
                    return false;
            }
        }

        public static List<CallOfDutyPlayerDataEntity> GetServersPlayerData(List<ulong> serverIds, string gameAbbrev, string modeAbbrev)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (gameAbbrev == null || modeAbbrev == null)
                {
                    return _db.CallOfDutyPlayerData
                        .AsQueryable()
                        .Where(player => serverIds.Contains(player.ServerID))
                        .AsEnumerable()
                        .ToList();
                }
                else
                {
                    return _db.CallOfDutyPlayerData
                        .AsQueryable()
                        .Where(player => serverIds.Contains(player.ServerID) && player.GameAbbrev == gameAbbrev && player.ModeAbbrev == modeAbbrev)
                        .AsEnumerable()
                        .ToList();
                }
            }
        }

        private static CallOfDutyPlayerDataEntity GetCallOfDutyPlayerDataEntity(ulong serverID, ulong discordID, string gameAbbrev, string modeAbbrev)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                return _db.CallOfDutyPlayerData
                    .AsQueryable()
                    .Where(player => player.ServerID == serverID && player.DiscordID == discordID && player.GameAbbrev == gameAbbrev && player.ModeAbbrev == modeAbbrev)
                    .SingleOrDefault();
            }
        }

        public static List<ulong> GetAllValidatedServerIds(string gameAbbrev, string modeAbbrev)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (gameAbbrev == "mw" && modeAbbrev == "mp")
                {
                    return _db.Servers
                        .AsQueryable()
                        .Where(s => s.AllowServerPermissionModernWarfareTracking && s.ToggleModernWarfareTracking)
                        .Select(s => s.ServerID)
                        .AsEnumerable()
                        .ToList();
                }
                else if (gameAbbrev == "mw" && modeAbbrev == "wz")
                {
                    return _db.Servers
                        .AsQueryable()
                        .Where(s => s.AllowServerPermissionWarzoneTracking && s.ToggleWarzoneTracking)
                        .Select(s => s.ServerID)
                        .AsEnumerable()
                        .ToList();
                }
                else if (gameAbbrev == "cw" && modeAbbrev == "mp")
                {
                    return _db.Servers
                        .AsQueryable()
                        .Where(s => s.AllowServerPermissionBlackOpsColdWarTracking && s.ToggleBlackOpsColdWarTracking)
                        .Select(s => s.ServerID)
                        .AsEnumerable()
                        .ToList();
                }
                else
                    return null;
            }
        }

        public static IMessageChannel GetServerCallOfDutyNotificationChannel(ulong serverId)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                var channelId = _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == serverId)
                    .Select(s => s.CallOfDutyNotificationChannelID)
                    .SingleOrDefault();

                if (channelId != 0)
                    return _client.GetChannel(channelId) as IMessageChannel;
                else
                    return null;
            }
        }

        public static ulong GetServerModernWarfareKillsRoleID(ulong serverId)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                return _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == serverId)
                    .Select(s => s.ModernWarfareKillsRoleID)
                    .SingleOrDefault();
            }
        }

        public static ulong GetServerWarzoneWinsRoleID(ulong serverId)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                return _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == serverId)
                    .Select(s => s.WarzoneWinsRoleID)
                    .SingleOrDefault();
            }
        }

        public static ulong GetServerWarzoneKillsRoleID(ulong serverId)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                return _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == serverId)
                    .Select(s => s.WarzoneKillsRoleID)
                    .SingleOrDefault();
            }
        }

        public static ulong GetServerBlackOpsColdWarKillsRoleID(ulong serverId)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                return _db.Servers
                    .AsQueryable()
                    .Where(s => s.ServerID == serverId)
                    .Select(s => s.BlackOpsColdWarKillsRoleID)
                    .SingleOrDefault();
            }
        }

        public static bool GetServerToggleBlackOpsColdWarTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.ToggleBlackOpsColdWarTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Black Ops Cold War commands will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }

        public static bool GetServerToggleModernWarfareTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.ToggleModernWarfareTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Modern Warfare commands will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }

        public static bool GetServerToggleWarzoneTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.ToggleWarzoneTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Warzone commands will be ignored: Admin toggled off. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }

        public static bool GetServerAllowServerPermissionBlackOpsColdWarTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.AllowServerPermissionBlackOpsColdWarTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Black Ops Cold War commands will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }

        public static bool GetServerAllowServerPermissionModernWarfareTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.AllowServerPermissionModernWarfareTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Modern Warfare commands will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }

        public static bool GetServerAllowServerPermissionWarzoneTracking(SocketCommandContext context)
        {
            using (StormBotContext _db = new StormBotContext())
            {
                if (!context.IsPrivate)
                {
                    bool flag = _db.Servers
                        .AsQueryable()
                        .Where(s => s.ServerID == context.Guild.Id)
                        .Select(s => s.AllowServerPermissionWarzoneTracking)
                        .Single();

                    if (!flag)
                        Console.WriteLine($"Warzone commands will be ignored: Bot ignoring server. Server: {context.Guild.Name} ({context.Guild.Id})");

                    return flag;
                }
                else
                    return true;
            }
        }
		#endregion
	}
}
