using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using StormBot.Models.CallOfDutyModels;
using StormBot.Database;
using StormBot.Database.Entities;

namespace StormBot.Services
{
	public class CallOfDutyService : BaseService
    {
        public readonly DiscordSocketClient _client;

        public BaseService BlackOpsColdWarComponent { get; set; }

        public BaseService ModernWarfareComponent { get; set; }

        public BaseService WarzoneComponent { get; set; }

        public CallOfDutyService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _db = services.GetRequiredService<StormBotContext>();

            Name = "Call Of Duty Service";
            isServiceRunning = false;

            BlackOpsColdWarComponent = new BaseService();
            BlackOpsColdWarComponent.Name = "Black Ops Cold War Service";
            BlackOpsColdWarComponent.isServiceRunning = false;

            ModernWarfareComponent = new BaseService();
            ModernWarfareComponent.Name = "Modern Warfare Service";
            ModernWarfareComponent.isServiceRunning = false;

            WarzoneComponent = new BaseService();
            WarzoneComponent.Name = "Warzone Service";
            WarzoneComponent.isServiceRunning = false;
        }

        public override async Task StartService()
        {
            string logStamp = GetLogStamp();

            if (!DoStart || !(BlackOpsColdWarComponent.DoStart || ModernWarfareComponent.DoStart || WarzoneComponent.DoStart))
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

                await BlackOpsColdWarComponent.StartService();
                await ModernWarfareComponent.StartService();
                await WarzoneComponent.StartService();

                List<ServersEntity> servers = await GetAllServerEntities();
                foreach (ServersEntity server in servers)
                {
                    string message = "";

                    if (BlackOpsColdWarComponent.isServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking)
                    {
                        message += "_**[    BLACK OPS COLD WAR TRACKING ONLINE.    ]**_\n";
                    }
                    if (ModernWarfareComponent.isServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking)
                    {
                        message += "_**[    MODERN WARFARE TRACKING ONLINE.    ]**_\n";
                    }
                    if (WarzoneComponent.isServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking)
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

            if (isServiceRunning)
            {
                Console.WriteLine(logStamp + "Stopping service.".PadLeft(68 - logStamp.Length));

                isServiceRunning = false;

                List<ServersEntity> servers = await GetAllServerEntities();
                foreach (ServersEntity server in servers)
                {
                    string message = "";

                    if (BlackOpsColdWarComponent.isServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking)
                    {
                        message += "_**[    BLACK OPS COLD WAR TRACKING DISCONNECTED.    ]**_\n";
                    }
                    if (ModernWarfareComponent.isServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking)
                    {
                        message += "_**[    MODERN WARFARE TRACKING DISCONNECTED.    ]**_\n";
                    }
                    if (WarzoneComponent.isServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking)
                    {
                        message += "_**[    WARZONE TRACKING DISCONNECTED.    ]**_";
                    }

                    if (server.CallOfDutyNotificationChannelID != 0 && message != "")
                        await ((IMessageChannel)_client.GetChannel(server.CallOfDutyNotificationChannelID)).SendMessageAsync(message);
                }

                await BlackOpsColdWarComponent.StopService();
                await ModernWarfareComponent.StopService();
                await WarzoneComponent.StopService();
            }
        }

        public void AddParticipantToDatabase(CallOfDutyPlayerDataEntity playerData)
        {
            _db.CallOfDutyPlayerData.Add(playerData);
            _db.SaveChangesAsync();
        }

        public void RemoveParticipantFromDatabase(CallOfDutyPlayerDataEntity playerData)
        {
            _db.CallOfDutyPlayerData.Remove(playerData);
            _db.SaveChangesAsync();
        }

        public async Task<List<CallOfDutyPlayerModel>> GetNewPlayerData(bool storeToDatabase, ulong serverId, string gameAbbrev, string modeAbbrev)
        {
            List<ulong> serverIds = new List<ulong>();
            serverIds.Add(serverId);

            // get all data of all players in the specified servers for the specified games
            List<CallOfDutyPlayerDataEntity> allStoredPlayersData = await GetServersPlayerData(serverIds, gameAbbrev, modeAbbrev);

            // if no player data is returned, return null
            if (allStoredPlayersData.Count == 0)
            {
                string logStamp = GetLogStamp();
                Console.WriteLine(logStamp + "Cannot retrieve player data for zero players.".PadLeft(126 - logStamp.Length));
                return null;
            }

            CookieContainer cookieJar = new CookieContainer();
            string XSRFTOKEN = "";

            // make initial request to get the XSRF-TOKEN
            InitializeAPI(ref cookieJar, ref XSRFTOKEN);

            // login to the API with credentials and XSRF-TOKEN to generate cookie tokens needed to retrieve player data
            LoginAPI(ref cookieJar, XSRFTOKEN);

            // retrieve updated data via Call of Duty API for all participating players with cookie tokens obtained from login
            List<CallOfDutyPlayerModel> newPlayerData = GetAllPlayersDataAPI(cookieJar, allStoredPlayersData, storeToDatabase);

            return newPlayerData;
        }

        private void InitializeAPI(ref CookieContainer cookieJar, ref string XSRFTOKEN)
        {
            RestClient client = new RestClient("https://profile.callofduty.com/null/login");
            client.CookieContainer = cookieJar;
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Cookie", "");
            client.UnsafeAuthenticatedConnectionSharing = true;
            client.Execute(request);

            XSRFTOKEN = client.CookieContainer.GetCookies(new Uri("https://profile.callofduty.com/null/login"))["XSRF-TOKEN"].Value;
        }

        private void LoginAPI(ref CookieContainer cookieJar, string XSRFTOKEN)
        {
            RestClient client = new RestClient("https://profile.callofduty.com/do_login?new_SiteId=cod");
            client.CookieContainer = cookieJar;
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("username", Program.configurationSettingsModel.ActivisionEmail);
            request.AddParameter("password", Program.configurationSettingsModel.ActivisionPassword);
            request.AddParameter("remember_me", "true");
            request.AddParameter("_csrf", XSRFTOKEN);
            client.Execute(request);
        }

        private List<CallOfDutyPlayerModel> GetAllPlayersDataAPI(CookieContainer cookieJar, List<CallOfDutyPlayerDataEntity> allStoredPlayersData, bool storeToDatabase)
        {
            // empty list of player data
            List<CallOfDutyPlayerModel> allNewPlayersData = new List<CallOfDutyPlayerModel>();

            // create CallOfDutyPlayerModel for each participating player from database with GetAPlayersData() & add each model to the allNewPlayerData list
            foreach (CallOfDutyPlayerDataEntity storedPlayerData in allStoredPlayersData)
            {
                CallOfDutyPlayerModel newPlayerData = GetAPlayersDataAPI(cookieJar, storedPlayerData);

                if (newPlayerData.Status == null || newPlayerData.Status != "success")
                {
                    string logStamp = GetLogStamp();
                    Console.WriteLine(logStamp + $"Error getting data for the Call of Duty account: ".PadLeft(100 - logStamp.Length) + storedPlayerData.Username);
                    return null;
                }

                if (storeToDatabase)
                {
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

                    _db.SaveChangesAsync();
                }

                newPlayerData.DiscordID = storedPlayerData.DiscordID;
                allNewPlayersData.Add(newPlayerData);
            }

            // create and return allPlayerDataModel with list
            return allNewPlayersData;
        }

        private CallOfDutyPlayerModel GetAPlayersDataAPI(CookieContainer cookieJar, CallOfDutyPlayerDataEntity storedPlayerData)
        {
            string gameAbbrev = storedPlayerData.GameAbbrev;
            string modeAbbrev = storedPlayerData.ModeAbbrev;
            string platform = storedPlayerData.Platform;
            string username = storedPlayerData.Username;
            string tag = "";

            if (storedPlayerData.Tag != "")
                tag = "%23" + storedPlayerData.Tag;

            RestClient client = new RestClient(string.Format(@"https://my.callofduty.com/api/papi-client/stats/cod/v1/title/{0}/platform/{1}/gamer/{2}{3}/profile/type/{4}", gameAbbrev, platform, username, tag, modeAbbrev));
            client.CookieContainer = cookieJar;
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            // convert API json response data into player data object
            return JsonConvert.DeserializeObject<CallOfDutyPlayerModel>(response.Content);
        }

        public async Task<List<CallOfDutyPlayerModel>> GetServersPlayerDataAsPlayerModelList(ulong serverId, string gameAbbrev, string modeAbbrev)
        {
            List<ulong> serverIdList = new List<ulong>();
            serverIdList.Add(serverId);

            List<CallOfDutyPlayerDataEntity> entityData = await GetServersPlayerData(serverIdList, gameAbbrev, modeAbbrev);

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

        public async Task<List<CallOfDutyPlayerDataEntity>> GetServersPlayerData(List<ulong> serverIds, string gameAbbrev, string modeAbbrev)
        {
            if (gameAbbrev == null || modeAbbrev == null)
            {
                return await _db.CallOfDutyPlayerData
                    .AsQueryable()
                    .Where(player => serverIds.Contains(player.ServerID))
                    .AsAsyncEnumerable()
                    .ToListAsync();
            }
            else
            {
                return await _db.CallOfDutyPlayerData
                    .AsQueryable()
                    .Where(player => serverIds.Contains(player.ServerID) && player.GameAbbrev == gameAbbrev && player.ModeAbbrev == modeAbbrev)
                    .AsAsyncEnumerable()
                    .ToListAsync();
            }
        }

        public async Task<List<ServersEntity>> GetAllServerEntities()
        {
            return await _db.Servers
                .AsQueryable()
                .AsAsyncEnumerable()
                .ToListAsync();
        }

        public async Task<List<ulong>> GetAllValidatedServerIds(string gameAbbrev, string modeAbbrev)
        {
            if (gameAbbrev == "mw" && modeAbbrev == "mp")
            {
                return await _db.Servers
                    .AsQueryable()
                    .Where(s => s.AllowServerPermissionModernWarfareTracking && s.ToggleModernWarfareTracking)
                    .Select(s => s.ServerID)
                    .AsAsyncEnumerable()
                    .ToListAsync();
            }
            else if (gameAbbrev == "mw" && modeAbbrev == "wz")
            {
                return await _db.Servers
                    .AsQueryable()
                    .Where(s => s.AllowServerPermissionWarzoneTracking && s.ToggleWarzoneTracking)
                    .Select(s => s.ServerID)
                    .AsAsyncEnumerable()
                    .ToListAsync();
            }
            else if (gameAbbrev == "cw" && modeAbbrev == "mp")
            {
                return await _db.Servers
                    .AsQueryable()
                    .Where(s => s.AllowServerPermissionBlackOpsColdWarTracking && s.ToggleBlackOpsColdWarTracking)
                    .Select(s => s.ServerID)
                    .AsAsyncEnumerable()
                    .ToListAsync();
            }
            else
                return null;
        }

        public async Task<IMessageChannel> GetServerCallOfDutyNotificationChannel(ulong serverId)
        {
            var channelId = await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == serverId)
                .Select(s => s.CallOfDutyNotificationChannelID)
                .SingleOrDefaultAsync();

            if (channelId != 0)
                return _client.GetChannel(channelId) as IMessageChannel;
            else
                return null;
        }

        public async Task<ulong> GetServerModernWarfareKillsRoleID(ulong serverId)
        {
            return await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == serverId)
                .Select(s => s.ModernWarfareKillsRoleID)
                .SingleOrDefaultAsync();
        }

        public async Task<ulong> GetServerWarzoneWinsRoleID(ulong serverId)
        {
            return await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == serverId)
                .Select(s => s.WarzoneWinsRoleID)
                .SingleOrDefaultAsync();
        }

        public async Task<ulong> GetServerWarzoneKillsRoleID(ulong serverId)
        {
            return await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == serverId)
                .Select(s => s.WarzoneKillsRoleID)
                .SingleOrDefaultAsync();
        }

        public async Task<ulong> GetServerBlackOpsColdWarKillsRoleID(ulong serverId)
        {
            return await _db.Servers
                .AsQueryable()
                .Where(s => s.ServerID == serverId)
                .Select(s => s.BlackOpsColdWarKillsRoleID)
                .SingleOrDefaultAsync();
        }
    }
}
