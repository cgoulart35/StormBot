using System;
using System.Threading.Tasks;
using cg_bot.Models.CallOfDutyModels.Players.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

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

		public IMessageChannel _callOfDutyNotificationChannelID;

		public AnnouncementsService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_callOfDutyNotificationChannelID = _client.GetChannel(Program.configurationSettingsModel.CallOfDutyNotificationChannelID) as IMessageChannel;

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
					await _callOfDutyNotificationChannelID.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S WINNERS!!!! CONGRATULATIONS!!!\n```");
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
				if (!dailySent && currentTime.Hour == 22 && currentTime.Minute == 0 && WeeklyCallOfDutyAnnouncement != null)
				{
					await _callOfDutyNotificationChannelID.SendMessageAsync("```fix\nHERE ARE THIS WEEK'S CURRENT RANKINGS!\n```");
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
