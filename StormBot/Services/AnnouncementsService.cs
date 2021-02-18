using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using StormBot.Database;
using StormBot.Database.Entities;
using System.Collections.Generic;
using Discord;

namespace StormBot.Services
{
	class AnnouncementsService : BaseService
	{
		public delegate Task OnCodAnnouncementHandler(object sender, EventArgs args);
		public event OnCodAnnouncementHandler WeeklyCallOfDutyAnnouncement;
		public event OnCodAnnouncementHandler DailyCallOfDutyAnnouncement;

		public delegate Task OnStormAnnouncementHandler(object sender, ulong serverId, string serverName, ulong channelId);
		public event OnStormAnnouncementHandler RandomStormAnnouncement;

		private bool weeklySent;
		private bool dailySent;

		private readonly DiscordSocketClient _client;

		private StormsService _stormsService;
		private CallOfDutyService _callOfDutyService;

		public AnnouncementsService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_stormsService = services.GetRequiredService<StormsService>();
			_callOfDutyService = services.GetRequiredService<CallOfDutyService>();
			_db = services.GetRequiredService<StormBotContext>();

			Name = "Announcements Service";
			isServiceRunning = false;
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
				weeklySent = false;
				dailySent = false;

				if (_stormsService.isServiceRunning)
				{
					List<ServersEntity> servers = await _stormsService.GetAllServerEntities();
					foreach (ServersEntity server in servers)
					{
						bool stormBotBool = server.AllowServerPermissionStorms && server.ToggleStorms;

						if (stormBotBool && server.StormsNotificationChannelID != 0)
						{
							StartStormAnnouncements(server);
						}
					}
				}

				if (_callOfDutyService.isServiceRunning)
				{
					StartCallOfDutyWeeklyAnnouncements();
					StartCallOfDutyDailyAnnouncements();
				}
			}
		}

		public async Task StartStormAnnouncements(ServersEntity server)
		{
			Random random = new Random();

			while (isServiceRunning && _stormsService.isServiceRunning)
			{
				string logStamp = GetLogStamp();

				// time between event invokes is between 1 hour and 4 hours (between 24 and 6 times a day)
				int randomTimeWait = random.Next(3600, 14401) * 1000;

				// time in milliseconds converted to hours minutes seconds
				int totalSeconds = randomTimeWait / 1000;
				int hours = totalSeconds / 3600;
				int minutes = (totalSeconds % 3600) / 60;
				int seconds = totalSeconds % 60;

				if (server.AllowServerPermissionStorms && server.ToggleStorms)
					Console.WriteLine(logStamp + $"			The next Storm in {server.ServerName} is in {hours} hours {minutes} minutes and {seconds} seconds.");

				await Task.Delay(randomTimeWait);

				if (server.AllowServerPermissionStorms && server.ToggleStorms)
					await RandomStormAnnouncement.Invoke(this, server.ServerID, server.ServerName, server.StormsNotificationChannelID);
			}
		}

		public async Task StartCallOfDutyWeeklyAnnouncements()
		{
			// send out weekly winners announcement at 1:00 AM (EST) on Sunday mornings
			while (isServiceRunning && _callOfDutyService.isServiceRunning)
			{
				DateTime currentTime = DateTime.Now;
				if (!weeklySent && currentTime.DayOfWeek == DayOfWeek.Sunday && currentTime.Hour == 1 && currentTime.Minute == 0 && WeeklyCallOfDutyAnnouncement != null)
				{
					List<ServersEntity> servers = await _callOfDutyService.GetAllServerEntities();
					foreach (ServersEntity server in servers)
					{
						bool coldWarBool = _callOfDutyService.BlackOpsColdWarComponent.isServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking;
						bool modernWarfareBool = _callOfDutyService.ModernWarfareComponent.isServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking;
						bool warzoneBool = _callOfDutyService.WarzoneComponent.isServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking;

						if ((coldWarBool || modernWarfareBool || warzoneBool) && server.CallOfDutyNotificationChannelID != 0)
						{
							await ((IMessageChannel)_client.GetChannel(server.CallOfDutyNotificationChannelID)).SendMessageAsync("```fix\nHERE ARE THIS WEEK'S WINNERS!!!! CONGRATULATIONS!!!\n```");
						}
					}

					await WeeklyCallOfDutyAnnouncement.Invoke(this, EventArgs.Empty);

					weeklySent = true;
				}
				else
					weeklySent = false;

				await Task.Delay(60000);
			}
		}

		public async Task StartCallOfDutyDailyAnnouncements()
		{
			// send out daily updates on current weekly kill counts at 10 PM (EST) everyday
			while (isServiceRunning && _callOfDutyService.isServiceRunning)
			{
				DateTime currentTime = DateTime.Now;
				if (!dailySent && currentTime.Hour == 22 && currentTime.Minute == 0 && DailyCallOfDutyAnnouncement != null)
				{
					List<ServersEntity> servers = await _callOfDutyService.GetAllServerEntities();
					foreach (ServersEntity server in servers)
					{
						bool coldWarBool = _callOfDutyService.BlackOpsColdWarComponent.isServiceRunning && server.AllowServerPermissionBlackOpsColdWarTracking && server.ToggleBlackOpsColdWarTracking;
						bool modernWarfareBool = _callOfDutyService.ModernWarfareComponent.isServiceRunning && server.AllowServerPermissionModernWarfareTracking && server.ToggleModernWarfareTracking;
						bool warzoneBool = _callOfDutyService.WarzoneComponent.isServiceRunning && server.AllowServerPermissionWarzoneTracking && server.ToggleWarzoneTracking;

						if ((coldWarBool || modernWarfareBool || warzoneBool) && server.CallOfDutyNotificationChannelID != 0)
						{
							await ((IMessageChannel)_client.GetChannel(server.CallOfDutyNotificationChannelID)).SendMessageAsync("```fix\nHERE ARE THIS WEEK'S CURRENT RANKINGS!\n```");
						}
					}

					await DailyCallOfDutyAnnouncement.Invoke(this, EventArgs.Empty);

					dailySent = true;
				}
				else
					dailySent = false;

				await Task.Delay(60000);
			}
		}
	}
}
