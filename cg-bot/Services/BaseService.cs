using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cg_bot.Services
{
	public class BaseService
	{
		public string Name { get; set; }

		public bool DoStart { get; set; }

		public bool isServiceRunning { get; set; }

		public virtual async Task StartService()
		{
			string logStamp = GetLogStamp();

			if (!DoStart)
			{
				Console.WriteLine(logStamp + "Disabled.".PadLeft(45-logStamp.Length));
			}
			else if (isServiceRunning)
			{
				Console.WriteLine(logStamp + "Service already running.".PadLeft(60 - logStamp.Length));
			}
			else
			{
				Console.WriteLine(logStamp + "Starting service.".PadLeft(53 - logStamp.Length));

				isServiceRunning = true;
			}
		}

		public string GetLogStamp()
		{
			return DateTime.Now.ToString("HH:mm:ss ") + GetType().Name + "     ";
		}
	}
}
