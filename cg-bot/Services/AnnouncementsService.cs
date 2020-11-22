using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace cg_bot.Services
{
	class AnnouncementsService : BaseService
	{
		public delegate Task OnAnnouncementHandler(object sender, EventArgs args);
		public event OnAnnouncementHandler CallOfDutyAnnouncement;

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

				await StartCallOfDutyAnnouncements();
			}
		}

		public async Task StartCallOfDutyAnnouncements()
		{
			// send out stats announcements request if 2:30 AM
			while (isServiceRunning)
			{
				DateTime currentTime = DateTime.Now;

				if (currentTime.Hour == 2 && currentTime.Minute == 30 && CallOfDutyAnnouncement != null)
				{
					string output = "" ;
					
					if (currentTime.DayOfWeek == DayOfWeek.Sunday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! THIS IS IT! ONE MORE DAY LEFT!!    ]**_\n__**Time Left:**__ 1 DAYS (24 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Saturday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! COME ON! TAKE THE LEAD!!    ]**_\n__**Time Left:**__ 2 DAYS (48 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Friday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! MORE THAN HALF WAY DONE!    ]**_\n__**Time Left:**__ 3 DAYS (72 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Thursday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! ALMOST HALF WAY TO THE FINISH LINE!    ]**_\n__**Time Left:**__ 4 DAYS (96 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Wednesday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! GET A HEAD START!    ]**_\n__**Time Left:**__ 5 DAYS (120 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Tuesday)
						output = "_**[    HERE ARE THIS WEEK'S CURRENT TOP PLAYERS! YOU GOT PLENTY OF TIME!    ]**_\n__**Time Left:**__ 6 DAYS (144 HOURS)";
					else if (currentTime.DayOfWeek == DayOfWeek.Monday)
						output = "_**[    HERE ARE THIS WEEK'S WINNERS!!!! CONGRATULATIONS!!!    ]**_\nThe next weekly competition starts in 30 minutes at 3:00 AM when all stats reset to zero.\n__**Time Left:**__ 7 DAYS (168 HOURS)";

					await _callOfDutyNotificationChannelID.SendMessageAsync(output);
					await CallOfDutyAnnouncement.Invoke(this, EventArgs.Empty);
				}
				
				await Task.Delay(60000);
			}
		}
	}
}
