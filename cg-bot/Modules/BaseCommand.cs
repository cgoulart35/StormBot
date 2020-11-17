using System;
using System.Collections.Generic;
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

        public List<string> ValidateOutputLimit(List<string> output, string messageToAdd)
        {
            string temp = output[output.Count - 1] + messageToAdd;
            if (temp.Length <= 2000)
            {
                output[output.Count - 1] += messageToAdd;
                return output;
            }
            else
            {
                output.Add("\n" + "...");
                return ValidateOutputLimit(output, messageToAdd);
            }
        }
    }
}
