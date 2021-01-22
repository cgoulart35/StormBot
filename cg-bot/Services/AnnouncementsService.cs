using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using cg_bot.Database;
using cg_bot.Database.Entities;
using System.Collections.Generic;
using Discord;

namespace cg_bot.Services
{
	class AnnouncementsService : BaseService
	{
		public delegate Task OnAnnouncementHandler(object sender, EventArgs args);
		public event OnAnnouncementHandler WeeklyCallOfDutyAnnouncement;
		public event OnAnnouncementHandler DailyCallOfDutyAnnouncement;

		private bool weeklySent;
		private bool dailySent;

		private readonly DiscordSocketClient _client;

		private CallOfDutyService _callOfDutyService;

		public AnnouncementsService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_callOfDutyService = services.GetRequiredService<CallOfDutyService>();
			_db = services.GetRequiredService<CgBotContext>();

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

				StartCallOfDutyWeeklyAnnouncements();
				StartCallOfDutyDailyAnnouncements();
			}
		}

		public async Task StartCallOfDutyWeeklyAnnouncements()
		{
			// send out weekly winners announcement at 1:00 AM (EST) on Sunday mornings
			while (isServiceRunning)
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
			while (isServiceRunning)
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
