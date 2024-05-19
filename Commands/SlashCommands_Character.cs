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
using System;

namespace TTRPGCreator.Commands
{
    public class SlashCommands_Character : ApplicationCommandModule
    {
        [SlashCommandGroup("Character", "Create or edit characters")]
        public class CharacterGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates a character or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("name", "Character name")] string name, [Option("description", "Character description")] string description, [Option("discord_id", "The Discord ID of whoever can control the character")] string discord_id = null, [Option("id", "The id of the character you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

                if(discord_id == "0") discord_id = ctx.User.Id.ToString();
                Character newCharacter = new Character
                {
                    id = id,
                    name = name,
                    description = description,
                    discord_id = long.TryParse(discord_id, out long temp) ? temp : (long?)null
                };

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddCharacter(ctx.Guild.Id, newCharacter);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Character added"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("List", "Lists all characters you have access to")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource); // Send initial response

                var interactivity = ctx.Client.GetInteractivity(); // Get the interactivity module

                var DBEngine = new DBEngine();
                var characters = await DBEngine.GetCharacters(ctx.Guild.Id);

                Console.WriteLine(characters.Count);

                var pages = new List<Page>(); // Create a list to hold your pages

                int pageCount = 1;
                for (int i = 0; i < characters.Count; i += 25) // Loop through characters, 25 at a time
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Page {pageCount++}",
                        Color = DiscordColor.Blue
                    };

                    for (int j = i; j < i + 25 && j < characters.Count; j++) // Add up to 25 characters to the embed
                    {
                        var character = characters[j];
                        string discordName = character.discord_id == null ? "" : Program.client.GetUserAsync((ulong)character.discord_id).Result.Username;
                        embed.AddField(character.name, $"ID: {character.id}\nDescription: {character.description}\nDiscord ID: {character.discord_id}\nDiscord Username: {discordName}");
                    }

                    pages.Add(new Page("", embed)); // Add the embed as a new page
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Character List")); // Edit the original response
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages); // Send the paginated message
            }

            [SlashCommand("Item", "Edits item information for a character")]
            public async Task Item(InteractionContext ctx, [Option("item", "Item id")] long item, [Option("character", "Character id")] long? character = null, [Option("quantity", "The number of the item to add. Negative if remove")] double quantity = 1, [Option("equipped", "If the item is equipped by the character")] NullBool equippedBool = NullBool.None, [Option("delete", "Use true to delete item from character")] bool delete = false)
            {
                await ctx.DeferAsync();

                bool? equipped;
                switch (equippedBool)
                {
                    case NullBool.True:
                        equipped = true;
                        break;
                    case NullBool.False:
                        equipped = false;
                        break;
                    default:
                        equipped = null;
                        break;
                }

                var DBEngine = new DBEngine();
                if (character == null)
                    character = (long?)await DBEngine.GetCharacterDiscord(ctx.Guild.Id, ctx.User.Id);

                int querySuccess = await DBEngine.AddCharacterItem(ctx.Guild.Id, (long)character, item, (int)quantity, equipped, delete);

                if (querySuccess == 1)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Item added to character"));
                else if (querySuccess == 2)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Item removed from character"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Status", "Edits status information for a character")]
            public async Task Status(InteractionContext ctx, [Option("status", "Status id")] long status, [Option("character", "Character id")] long? character = null, [Option("level", "The level of the status")] double level = 1, [Option("delete", "Use true to delete status from character")] bool delete = false)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                if (character == null)
                    character = (long?)await DBEngine.GetCharacterDiscord(ctx.Guild.Id, ctx.User.Id);

                int querySuccess = await DBEngine.AddCharacterStatus(ctx.Guild.Id, (long)character, status, (int)level, delete);

                if (querySuccess == 1)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status added to character"));
                else if (querySuccess == 2)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status removed from character"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Get", "Get information of a character")]
            public async Task Get(InteractionContext ctx, [Option("character", "Character id")] long? characterId = null)
            {
                // Defer the reply. This is especially useful for longer running commands
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                if (characterId == null)
                    characterId = (long?)await DBEngine.GetCharacterDiscord(ctx.Guild.Id, ctx.User.Id);

                var characterInfo = await DBEngine.GetCharacter(ctx.Guild.Id, (long)characterId, true);
                if (!characterInfo.Item1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
                    return;
                }

                Character character = characterInfo.Item2;

                // Create a new embed builder
                var embed = new DiscordEmbedBuilder
                {
                    Title = character.name,
                    Description = character.description,
                    Color = DiscordColor.Blurple // You can set the embed color here
                };

                string discordName = character.discord_id == null? "" : Program.client.GetUserAsync((ulong)character.discord_id).Result.Username;
                embed.AddField("Info:", $"ID: {character.id}\nDiscord ID: {character.discord_id}\nDiscord Username: {discordName}");

                if(character.items != null)
                    foreach (Item item in character.items)
                        embed.AddField("Item: " + item.name, $"ID: {item.id}\nDescription: {item.description}\nQuantity: {item.quantity}\nEquipped: {item.equipped}");

                if (character.statuses != null)
                    foreach (Status status in character.statuses)
                        embed.AddField("Status: " + status.name, $"ID: {status.id}\nDescription: {status.description}\nType: {status.type}");

                // Edit the original deferred message with the new embed
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.Build()));
            }

            [SlashCommand("Delete", "Deletes a character")]
            public async Task Delete(InteractionContext ctx, [Option("character", "Character id")] long character)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.DeleteCharacter(ctx.Guild.Id, character);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Character deleted"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }
        }
    }
}