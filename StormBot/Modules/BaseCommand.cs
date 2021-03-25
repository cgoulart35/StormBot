using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Discord.Addons.Interactive;
using Discord.Commands;
using StormBot.Services;

namespace StormBot.Modules
{
	public class BaseCommand : InteractiveBase<SocketCommandContext>
	{
        public static string GetSingleArg(string[] args)
		{
			return args.Any() ? string.Join(" ", args) : null;
		}

        public ulong GetDiscordID(string input, bool isUser = true, bool isChannel = true)
        {
            // if no name given
            if (input == null)
                throw new Exception();

            string trimmedInput = "";

            // if username is in <@!xxxxxxxxxxxxxxxxxx> format
            if (input.Length == 22)
                trimmedInput = input.Substring(3, 18);

            // if username is in <@xxxxxxxxxxxxxxxxxx> format
            else if (input.Length == 21)
                trimmedInput = input.Substring(2, 18);

            ulong discordID = Convert.ToUInt64(trimmedInput);

            if (isUser)
            {
                // if user exists in the server
                if (Context.Guild.GetUser(discordID) == null)
                    throw new Exception();
            }
            else
            {
                if (isChannel)
                {
                    // if channel exists in the server
                    if (Context.Guild.GetChannel(discordID) == null)
                        throw new Exception();
                }
                else
                {
                    // if role exists in the server
                    if (Context.Guild.GetRole(discordID) == null)
                        throw new Exception();
                }
            }

            return discordID;
        }

        public static bool ImageExistsAtURL(string url)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "HEAD";

            try
            {
                request.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DisableIfServiceNotRunning(BaseService service, string command = null)
        {
            string serviceName = service.Name;
            bool isRunning = service.IsServiceRunning;
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

        public static List<string> ValidateOutputLimit(List<string> output, string messageToAdd)
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
