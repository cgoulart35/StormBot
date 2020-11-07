using System;
using cg_bot.Services;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace cg_bot.Modules
{
	public class BaseCommand : InteractiveBase<SocketCommandContext>
	{
        public string GetSingleArg(string[] args)
		{
			return args.Length != 0 ? string.Join(" ", args) : null;
		}

        public bool DisableIfServiceNotRunning(BaseService service, string command = null)
        {
            string serviceName = service.Name;
            bool isRunning = service.isServiceRunning;
            if (isRunning == false)
            {
                // if the call is being used to disable a command
                if (command != null)
                {
                    Console.WriteLine($"Command [{command}] will be ignored: {serviceName} not running.");
                }
                
                // disabled
                return false;
            }
            else
            {
                // enabled
                return true;
            }
        }
    }
}
