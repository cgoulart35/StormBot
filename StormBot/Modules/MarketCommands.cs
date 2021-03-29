using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using StormBot.Services;
using StormBot.Models.MarketModels;

namespace StormBot.Modules
{
	public class MarketCommands : BaseCommand
	{
		private readonly MarketService _service;

        public MarketCommands(IServiceProvider services)
        {
            _service = services.GetRequiredService<MarketService>();
        }

        #region COMMANDS
        [Command("craft", RunMode = RunMode.Async)]
        public async Task CraftCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "craft"))
                    {
                        if (args.Length >= 3)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;

                            string imageURL = args[0];
                            string priceStr = args[1];

                            string[] itemNameArgs = new string[args.Length - 2];
                            Array.Copy(args, 2, itemNameArgs, 0, args.Length - 2);
                            string itemName = GetSingleArg(itemNameArgs);

                            MarketService.AddPlayerToDbTableIfNotExist(discordId);

                            // if price is a number
                            if (double.TryParse(priceStr, out double price))
                            {
                                if (price <= 0)
                                    await ReplyAsync($"<@!{discordId}>, please set a price above zero.");
                                else if (price > StormsService.resetMark)
                                    await ReplyAsync($"<@!{discordId}>, please set a price below the reset mark ({StormsService.resetMark} points).");
                                else if (price / 5 > StormsService.GetPlayerWallet(serverId, discordId))
                                    await ReplyAsync($"<@!{discordId}>, you have insufficient funds. You will be charged 20% of the listed item price for manufacturing.");
                                else if (!ImageExistsAtURL(imageURL))
                                    await ReplyAsync($"<@!{discordId}>, please provide a valid image URL.");
                                else if (MarketService.GetPlayerMarketItem(discordId, itemName) != null)
                                    await ReplyAsync($"<@!{discordId}>, there is already an item with this name in your inventory.");
                                else
                                {
                                    MarketItemModel newItem = new MarketItemModel()
                                    {
                                        Name = itemName,
                                        Price = price,
                                        ImageURL = imageURL,
                                        HasBeenSold = false
                                    };

                                    if (MarketService.AddPlayerMarketItem(discordId, newItem))
                                    {
                                        StormsService.SubtractPointsFromPlayersWallet(serverId, discordId, price / 5);
                                        await ReplyAsync($"<@!{discordId}>, the item was crafted for {price / 5} points.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("dismantle", RunMode = RunMode.Async)]
        public async Task DismantleCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "dismantle"))
                    {
                        if (args.Length >= 1)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;
                            string itemName = GetSingleArg(args);

                            MarketService.AddPlayerToDbTableIfNotExist(discordId);
                            MarketItemModel item = MarketService.GetPlayerMarketItem(discordId, itemName);

                            if (item != null)
                            {
                                if (item.HasBeenSold)
                                {
                                    // give user full price back from inventory if it exists, has been sold, and was removed
                                    if (MarketService.RemovePlayerMarketItem(discordId, itemName) >= 1)
                                    {
                                        StormsService.AddPointsToPlayersWallet(serverId, discordId, item.Price);
                                        await ReplyAsync($"<@!{discordId}>, the item has been dismantled for {item.Price} points.");
                                    }
                                }
                                else
                                    await ReplyAsync($"<@!{discordId}>, can't dismantle items that have not been sold.");
                            }
                            else
                                await ReplyAsync($"<@!{discordId}>, no item exists.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("rename", RunMode = RunMode.Async)]
        public async Task RenameCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "rename"))
                    {
                        if (args.Length >= 1)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;
                            string itemName = GetSingleArg(args);

                            MarketService.AddPlayerToDbTableIfNotExist(discordId);
                            MarketItemModel item = MarketService.GetPlayerMarketItem(discordId, itemName);

                            if (item != null)
                            {
                                await ReplyAsync($"What would you like to rename this item to? Please answer with a name or 'cancel'.");
                                var userSelectResponse = await NextMessageAsync(true, true, new TimeSpan(0, 1, 0));

                                // if user responds in time
                                if (userSelectResponse != null)
                                {
                                    // if nothing, don't do anything
                                    if (userSelectResponse.Content == null)
                                    {

                                    }
                                    // if response is cancel display cancelled message
                                    else if (userSelectResponse.Content.ToLower() == "cancel")
                                    {
                                        await ReplyAsync("Request cancelled.");
                                    }
                                    // if same user starts another command while awaiting a response, end this one but don't display request cancelled
                                    else if (userSelectResponse.Content.StartsWith(BaseService.GetServerOrPrivateMessagePrefix(Context)))
                                    {
                                    }
                                    else if (MarketService.UpdatePlayerMarketItemName(discordId, itemName, userSelectResponse.Content))
                                    {
                                        await ReplyAsync($"The item was renamed to {userSelectResponse.Content}.");
                                    }
                                }
                                // if user doesn't respond in time
                                else
                                {
                                    await ReplyAsync("You did not reply before the timeout.");
                                }
                            }
                            else
                                await ReplyAsync($"<@!{discordId}>, no item exists.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("buy", RunMode = RunMode.Async)]
        public async Task BuyCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "buy"))
                    {
                        if (args.Length >= 2)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;

                            try
                            {
                                ulong ownerDiscordId = GetDiscordID(args[0]);

                                string[] itemNameArgs = new string[args.Length - 1];
                                Array.Copy(args, 1, itemNameArgs, 0, args.Length - 1);
                                string itemName = GetSingleArg(itemNameArgs);

                                MarketService.AddPlayerToDbTableIfNotExist(discordId);

                                if (discordId != ownerDiscordId)
                                {
                                    MarketItemModel item = MarketService.GetPlayerMarketItem(ownerDiscordId, itemName);

                                    if (item != null)
                                    {
                                        if (StormsService.GetPlayerWallet(serverId, discordId) < item.Price)
                                            await ReplyAsync($"<@!{discordId}>, you have insufficient funds.");
                                        else if (MarketService.GetPlayerMarketItem(discordId, itemName) != null)
                                            await ReplyAsync($"<@!{discordId}>, there is already an item with this name in your inventory.");
                                        else
                                        {
                                            if (MarketService.OpenPendingTransaction(serverId, discordId, ownerDiscordId, itemName))
                                            {
                                                await ReplyAsync($"<@!{discordId}>, your request is awaiting approval from <@!{ownerDiscordId}>.");

                                                // create timer to keep track of elapsed time
                                                Stopwatch timer = new Stopwatch();
                                                timer.Start();

                                                // wait for an approval or timeout
                                                while (MarketService.GetTransactionStatus(serverId, discordId, ownerDiscordId, itemName).Value == -1 && timer.Elapsed.TotalMinutes < 5) { }

                                                timer.Stop();

                                                if (MarketService.GetTransactionStatus(serverId, discordId, ownerDiscordId, itemName).Value == 1)
                                                {
                                                    if (StormsService.GetPlayerWallet(serverId, discordId) < item.Price)
                                                        await ReplyAsync($"<@!{discordId}>, you have insufficient funds.");
                                                    else if (MarketService.GetPlayerMarketItem(discordId, itemName) != null)
                                                        await ReplyAsync($"<@!{discordId}>, there is already an item with this name in your inventory.");
                                                    else
                                                    {
                                                        item.HasBeenSold = true;

                                                        if (MarketService.AddPlayerMarketItem(discordId, item) && MarketService.RemovePlayerMarketItem(ownerDiscordId, itemName) >= 1)
                                                        {
                                                            StormsService.AddPointsToPlayersWallet(serverId, ownerDiscordId, item.Price);
                                                            StormsService.SubtractPointsFromPlayersWallet(serverId, discordId, item.Price);

                                                            await ReplyAsync($"<@!{discordId}>, you have purchased {itemName} from <@!{ownerDiscordId}> for {item.Price} points.");
                                                        }
                                                    }
                                                }
                                                else
                                                    await ReplyAsync($"<@!{discordId}>, your request was not approved before the timeout.");

                                                MarketService.ClosePendingTransaction(serverId, discordId, ownerDiscordId, itemName);
                                            }
                                        }
                                    }
                                    else
                                        await ReplyAsync($"<@!{discordId}>, no item exists.");
                                }
                                else
                                    await ReplyAsync($"<@!{discordId}>, you cannot buy from yourself.");
                            }
                            catch
                            {
                                await ReplyAsync("Please provide a valid Discord user.");
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("sell", RunMode = RunMode.Async)]
        public async Task SellCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "sell"))
                    {
                        if (args.Length >= 2)
                        {
                            ulong serverId = Context.Guild.Id;
                            ulong discordId = Context.User.Id;

                            try
                            {
                                ulong buyerDiscordId = GetDiscordID(args[0]);

                                string[] itemNameArgs = new string[args.Length - 1];
                                Array.Copy(args, 1, itemNameArgs, 0, args.Length - 1);
                                string itemName = GetSingleArg(itemNameArgs);

                                MarketService.AddPlayerToDbTableIfNotExist(discordId);
                                int? status = MarketService.GetTransactionStatus(serverId, buyerDiscordId, discordId, itemName);

                                if (discordId != buyerDiscordId)
                                {
                                    if (status.HasValue && status.Value == -1)
                                    {
                                        MarketService.UpdateTransactionStatusSold(serverId, buyerDiscordId, discordId, itemName);
                                    }
                                    else
                                        await ReplyAsync($"<@!{discordId}>, <@!{buyerDiscordId}> is not awaiting a sale approval for that item.");
                                }
                                else
                                    await ReplyAsync($"<@!{discordId}>, you cannot sell to yourself.");
                            }
                            catch
                            {
                                await ReplyAsync("Please provide a valid Discord user.");
                            }
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("item", RunMode = RunMode.Async)]
        public async Task ItemCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "item"))
                    {
                        if (args.Length >= 1)
                        {
                            ulong discordId = Context.User.Id;
                            string itemName = GetSingleArg(args);

                            MarketService.AddPlayerToDbTableIfNotExist(discordId);
                            MarketItemModel item = MarketService.GetPlayerMarketItem(discordId, itemName);

                            if (item != null)
                            {
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Gold);
                                builder.WithTitle($"**{item.Name}**");
                                builder.WithThumbnailUrl(Context.User.GetAvatarUrl());
                                builder.WithDescription($"Owned by {Context.User.Username}.");
                                builder.AddField("Price", $"`{item.Price}`", false);
                                builder.AddField("Can Dismantle", item.HasBeenSold ? "`Yes`" : "`No`", false);
                                builder.WithImageUrl(item.ImageURL);                                

                                await ReplyAsync("", false, builder.Build());
                            }
                            else
                                await ReplyAsync($"<@!{discordId}>, no item exists.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }

        [Command("items", RunMode = RunMode.Async)]
        public async Task ItemsCommand(params string[] args)
        {
            if (!Context.IsPrivate)
            {
                if (MarketService.GetServerAllowServerPermissionMarket(Context) && MarketService.GetServerToggleMarket(Context))
                {
                    if (DisableIfServiceNotRunning(_service, "items"))
                    {
                        ulong serverId = Context.Guild.Id;
                        ulong discordId;

                        try
                        {
                            if (args.Length == 1)
                            {
                                string input = GetSingleArg(args);
                                discordId = GetDiscordID(input);
                            }
                            else
                                discordId = Context.User.Id;

                            MarketService.AddPlayerToDbTableIfNotExist(discordId);
                            MarketItemsModel marketItemsModel = MarketService.GetAllPlayerMarketItems(discordId);

                            if (marketItemsModel != null && marketItemsModel.Items != null && marketItemsModel.Items.Count > 0)
                            {
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Gold);
                                builder.WithTitle($"**{Context.Guild.GetUser(discordId).Username}'s Inventory**");
                                builder.WithThumbnailUrl(Context.Guild.GetUser(discordId).GetAvatarUrl());

                                string itemsStr = "";
                                string pricesStr = "";
                                foreach (MarketItemModel item in marketItemsModel.Items.OrderByDescending(item => item.Price))
                                {
                                    itemsStr += $"`{item.Name}`\n";
                                    pricesStr += $"`{item.Price}`\n";
                                }

                                builder.AddField("Item", itemsStr, true);
                                builder.AddField("Price", pricesStr, true);

                                await ReplyAsync("", false, builder.Build());
                            }
                            else
                            {
                                if (discordId != Context.User.Id)
                                    await ReplyAsync($"<@!{discordId}> has no items.");
                                else
                                    await ReplyAsync($"<@!{discordId}>, you have no items.");
                            }
                        }
                        catch
                        {
                            await ReplyAsync("Please provide a valid Discord user.");
                        }
                    }
                }
            }
            else
                await ReplyAsync("This command can only be executed in servers.");
        }
        #endregion
    }
}
