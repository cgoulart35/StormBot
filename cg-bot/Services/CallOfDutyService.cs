using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using cg_bot.Models.CallOfDutyModels.Accounts;
using cg_bot.Models.CallOfDutyModels.Players;
using cg_bot.Models.CallOfDutyModels.Players.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestSharp;

namespace cg_bot.Services
{
	public class CallOfDutyService<T> : BaseService
    {
        public readonly DiscordSocketClient _client;

        public IMessageChannel _callOfDutyNotificationChannelID;

        public ICallOfDutyDataModel _dataModel;

        string pathGameName;

        public static string cgBotCallOfDutyGameParticipatingAccountsDataPath;
        public static string cgBotCallOfDutyGameSavedPlayerDataPath;

        public CallOfDutyAllPlayersModel<T> storedPlayerDataModel;
        public CallOfDutyAllPlayersModel<T> newPlayerDataModel;

        public CallOfDutyService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _callOfDutyNotificationChannelID = _client.GetChannel(Program.configurationSettingsModel.CallOfDutyNotificationChannelID) as IMessageChannel;

            _dataModel = Activator.CreateInstance<T>() as ICallOfDutyDataModel;

            Name = _dataModel.GameName + " Service";

            isServiceRunning = false;

            _dataModel.ParticipatingAccountsFileLock = new object();
            _dataModel.SavedPlayerDataFileLock = new object();

            pathGameName = _dataModel.GameName.Replace(" ", string.Empty);

            cgBotCallOfDutyGameParticipatingAccountsDataPath = Path.Combine(Program.cgBotAppDataPath, string.Format(@"{0}ParticipatingAccounts.json", pathGameName));
            cgBotCallOfDutyGameSavedPlayerDataPath = Path.Combine(Program.cgBotAppDataPath, string.Format(@"{0}SavedPlayerData.json", pathGameName));
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

                await _callOfDutyNotificationChannelID.SendMessageAsync("_**[    " + _dataModel.GameName.ToUpper() + " TRACKING ONLINE.    ]**_");

                // get all previously stored data
                try
                {
                    storedPlayerDataModel = ImportSavedPlayerData();
                }
                catch
                {
                    Console.WriteLine(logStamp + string.Format("Could not get previously stored data. The file {0}SavedPlayerData.json was corrupt or empty. The file will be overwritten when new data is retrieved.", pathGameName).PadLeft(197 + pathGameName.Length - logStamp.Length));
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

                await _callOfDutyNotificationChannelID.SendMessageAsync("_**[    " + _dataModel.GameName.ToUpper() + " TRACKING DISCONNECTED.    ]**_");
            }
        }

        private CallOfDutyAllPlayersModel<T> ImportSavedPlayerData()
        {
            lock (_dataModel.SavedPlayerDataFileLock)
            {
                CallOfDutyAllPlayersModel<T> playerData = JsonConvert.DeserializeObject<CallOfDutyAllPlayersModel<T>>(File.ReadAllText(cgBotCallOfDutyGameSavedPlayerDataPath));
                return playerData;
            }
        }

        public void AddParticipantToFile(CallOfDutyAccountModel account)
        {
            CallOfDutyAllAccountsModel participatingAccountsData = ReadParticipatingAccounts();
            participatingAccountsData.Accounts.Add(account);
            ExportParticipatingAccounts(participatingAccountsData);
        }

        public void RemoveParticipantFromFile(CallOfDutyAccountModel account)
        {
            CallOfDutyAllAccountsModel participatingAccountsData = ReadParticipatingAccounts();
            participatingAccountsData.Accounts.RemoveAll(accounts => accounts.DiscordID == account.DiscordID);
            ExportParticipatingAccounts(participatingAccountsData);
        }

        private void ExportParticipatingAccounts(CallOfDutyAllAccountsModel participatingAccountsData)
        {
            lock (_dataModel.ParticipatingAccountsFileLock)
            {
                File.WriteAllText(cgBotCallOfDutyGameParticipatingAccountsDataPath, JsonConvert.SerializeObject(participatingAccountsData));
            }
        }

        public CallOfDutyAllPlayersModel<T> GetNewPlayerData(bool overwriteStoredData = false)
        {
            // get list of participating players
            CallOfDutyAllAccountsModel participatingAccountsData = ReadParticipatingAccounts();

            // if no player data is returned, return null
            if (participatingAccountsData.Accounts.Count == 0)
            {
                string logStamp = GetLogStamp();
                Console.WriteLine(logStamp + "Cannot retrieve player data for zero participants. Please add participants.".PadLeft(126 - logStamp.Length));
                return null;
            }

            CookieContainer cookieJar = new CookieContainer();
            string XSRFTOKEN = "";

            // make initial request to get the XSRF-TOKEN
            InitializeAPI(ref cookieJar, ref XSRFTOKEN);

            // login to the API with credentials and XSRF-TOKEN to generate cookie tokens needed to retrieve player data
            LoginAPI(ref cookieJar, XSRFTOKEN);

            // retrieve updated data via Call of Duty: Modern Warefare API for all participating players with cookie tokens obtained from login
            newPlayerDataModel = GetAllPlayersDataAPI(cookieJar, participatingAccountsData);

            if (overwriteStoredData)
            {
                // keep track of the last retrieved data by setting the new data ro the stored data (only if new data has already been retrieved)
                if (newPlayerDataModel != null)
                    storedPlayerDataModel = newPlayerDataModel;

                // store the data in the json file
                ExportNewPlayerData();
            }

            return newPlayerDataModel;
        }

        public CallOfDutyAllAccountsModel ReadParticipatingAccounts()
        {
            lock (_dataModel.ParticipatingAccountsFileLock)
            {
                try
                {
                    CallOfDutyAllAccountsModel accountData = JsonConvert.DeserializeObject<CallOfDutyAllAccountsModel>(File.ReadAllText(cgBotCallOfDutyGameParticipatingAccountsDataPath));
                    
                    // if file empty
                    if (accountData == null)
                        throw new Exception();

                    foreach (CallOfDutyAccountModel account in accountData.Accounts)
                    {
                        if (account.DiscordID == 0 || account.Username == null || account.Username == "" || account.Platform == null || account.Platform == "")
                            throw new Exception();
                    }

                    return accountData;
                }
                catch
                {
                    // if error parsing participating accounts or if required data is null, return no accounts
                    CallOfDutyAllAccountsModel noAccountData = new CallOfDutyAllAccountsModel();
                    noAccountData.Accounts = new List<CallOfDutyAccountModel>();
                    return noAccountData;
                }
            }
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

        private CallOfDutyAllPlayersModel<T> GetAllPlayersDataAPI(CookieContainer cookieJar, CallOfDutyAllAccountsModel participatingAccountsData)
        {
            // empty list of player data
            List<CallOfDutyPlayerModel<T>> allPlayerData = new List<CallOfDutyPlayerModel<T>>();

            // create ModernWarfarePlayerDataModel for each participating player with GetAPlayersData() & add each model to the allPlayerData list
            foreach (CallOfDutyAccountModel account in participatingAccountsData.Accounts)
            {
                CallOfDutyPlayerModel<T> playerData = GetAPlayersDataAPI(cookieJar, account);
                playerData.DiscordID = account.DiscordID;
                playerData.Date = DateTime.Now;
                allPlayerData.Add(playerData);
            }

            // create and return allPlayerDataModel with list
            return new CallOfDutyAllPlayersModel<T>(allPlayerData);
        }

        private CallOfDutyPlayerModel<T> GetAPlayersDataAPI(CookieContainer cookieJar, CallOfDutyAccountModel account)
        {
            string platform = account.Platform;
            string username = account.Username;
            string tag = "";

            if (account.Tag != "")
                tag = "%23" + account.Tag;

            RestClient client = new RestClient(string.Format(@"https://my.callofduty.com/api/papi-client/stats/cod/v1/title/{0}/platform/{1}/gamer/{2}{3}/profile/type/{4}", _dataModel.GameAbbrevAPI, platform, username, tag, _dataModel.ModeAbbrevAPI));
            client.CookieContainer = cookieJar;
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            // convert API json response into player data object
            return JsonConvert.DeserializeObject<CallOfDutyPlayerModel<T>>(response.Content);
        }

        private void ExportNewPlayerData()
        {
            lock (_dataModel.SavedPlayerDataFileLock)
            {
				File.WriteAllText(cgBotCallOfDutyGameSavedPlayerDataPath, JsonConvert.SerializeObject(newPlayerDataModel));
            }
        }
    }
}
