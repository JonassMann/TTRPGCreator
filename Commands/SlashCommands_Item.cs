using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using TTRPGCreator.Functionality;
using TTRPGCreator.Database;
using DSharpPlus.CommandsNext;
using TTRPGCreator.System;
using DSharpPlus.CommandsNext.Attributes;
using System.Collections.Generic;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;

namespace TTRPGCreator.Commands
{
    public class SlashCommands_Item : ApplicationCommandModule
    {
        [SlashCommandGroup("Item", "Create or edit items")]
        public class ItemGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates an item or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("name", "Item name")] string name, [Option("description", "Item description")] string description, [Option("id", "The id of the item you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

                Item newItem = new Item
                {
                    id = id,
                    name = name,
                    description = description
                };

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddItem(ctx.Guild.Id, newItem);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Item added"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("List", "Lists all items you have access to")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource); // Send initial response

                var interactivity = ctx.Client.GetInteractivity(); // Get the interactivity module

                var DBEngine = new DBEngine();
                var items = await DBEngine.GetItems(ctx.Guild.Id);

                var pages = new List<Page>(); // Create a list to hold your pages

                int pageCount = 1;
                for (int i = 0; i < items.Count; i += 25) // Loop through characters, 25 at a time
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Page {pageCount++}",
                        Color = DiscordColor.Blue
                    };

                    for (int j = i; j < i + 25 && j < items.Count; j++) // Add up to 25 characters to the embed
                    {
                        var item = items[j];
                        embed.AddField(item.name, $"ID: {item.id}\nDescription: {item.description}");
                    }

                    pages.Add(new Page("", embed)); // Add the embed as a new page
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Item List")); // Edit the original response
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages); // Send the paginated message
            }

            [SlashCommand("Status", "Edits status information for an item")]
            public async Task Status(InteractionContext ctx, [Option("item", "Item id")] long item, [Option("status", "Status id")] long status, [Option("level", "The level of the status")] double level = 1)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddItemStatus(ctx.Guild.Id, item, status, (int)level);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status added to item"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }
        }
    }
}