using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace cg_bot.Modules
{
    public class GeneralCommands : BaseCommand
    {
        /*
        [Command("play battleship with", RunMode = RunMode.Async)]
        public async Task BattleshipCommand(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();

            try
            {
                string input = GetSingleArg(args);

                ulong discordID = GetDiscordUserID(input);

                // make sure initiating discord user and provided discord user are both in different colored teams
                // make sure only players allowed to respond, 5 minute wait or game aborted; different spots that don't over lap; smaller ships and grid?
                // add to a currently playing queue/object/list; grid with marked hits, misses, and ships; check every play for H, M, S, and E markers on players' grids
                // message provided user in their teams's channel that initiator is placing ships
                // prompt initiator in initator's channel to place ships
                // message initiator in their team' channel that provided user is placing ships
                // prompt provided user in their team's channel to place ships
                // initiator's first move; show previous hits and misses
                // message other user to wait
                // provided user's first move; show previous hits and misses
                // message other user to wait
                // once all ships sunk, player still standing wins

            }
            catch
            {
                await ReplyAsync("Please provide a valid Discord user.");
            }
        }
        */
    }
}
