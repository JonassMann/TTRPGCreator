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
    public class SlashCommands_System : ApplicationCommandModule
    {
        [SlashCommandGroup("Game", "Manipulates game information")]
        public class GameGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Create a new game for the server")]
            public async Task GameCreate(InteractionContext ctx, [Option("gameName", "Name of the game to create")] string gameName)
            {
                var DBEngine = new DBEngine();

                var games = await DBEngine.GetGames(ctx.Guild.Id);
                if (games != null)
                {
                    if (games.Contains(gameName))
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Game with name {gameName} already exists"));
                        return;
                    }

                    var modal = new DiscordInteractionResponseBuilder()
                        .WithTitle("Create new game")
                        .WithCustomId("system_gameCreate")
                        .AddComponents(new TextInputComponent("Game name", "gameName", null, gameName))
                        .AddComponents(new TextInputComponent("Default Dice", "defaultDice", null, "1d20"))
                        .AddComponents(new TextInputComponent("Character Stats", "characterStats", null, "Strength;Str|Dexterity;Dex|Constitution;Con|Intelligence;Int|Wisdom;Wis|Charisma;Cha"))
                        .AddComponents(new TextInputComponent("Stat modifier formula", "statFormula", null, "(x/2)-5"));

                    await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
                }
                else
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Run", "Selects the game to run")]
            public async Task GameRun(InteractionContext ctx)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();

                var games = await DBEngine.GetGames(ctx.Guild.Id);
                if (games != null)
                {
                    string descriptionText;
                    if (DataCache.gameList.ContainsKey(ctx.Guild.Id))
                    {
                        string gameName = DataCache.gameList[ctx.Guild.Id].Substring($"\"{ctx.Guild.Id}_".Length, DataCache.gameList[ctx.Guild.Id].Length - $"\"{ctx.Guild.Id}_".Length - 1);
                        descriptionText = $"Current game: {gameName}";
                    }
                    else
                        descriptionText = "Currently no game selected";

                    if (games.Count == 0)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"No games found\n\n{descriptionText}"));
                        return;
                    }
                    else if (games.Count > 20)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Too many games found\n\n{descriptionText}"));
                        return;
                    }

                    var gamesEmbed = new DiscordEmbedBuilder
                    {
                        Title = $"Games for server {ctx.Guild.Name}",
                        Description = descriptionText,
                        Color = DiscordColor.Gold
                    };

                    var message = new DiscordWebhookBuilder().AddEmbed(gamesEmbed);

                    List<DiscordButtonComponent> buttons = new List<DiscordButtonComponent>();
                    foreach (string game in games)
                    {
                        DiscordButtonComponent button;

                        if (DataCache.gameList.ContainsKey(ctx.Guild.Id) && DataCache.gameList[ctx.Guild.Id] == $"\"{ctx.Guild.Id}_{game}\"")
                            button = new DiscordButtonComponent(ButtonStyle.Secondary, $"system_gameChange{game}", game, true);
                        else
                            button = new DiscordButtonComponent(ButtonStyle.Primary, $"system_gameChange{game}", game);

                        buttons.Add(button);
                        if (buttons.Count == 5)
                        {
                            message.AddComponents(buttons);
                            buttons.Clear();
                        }
                    }

                    if (buttons.Count > 0)
                        message.AddComponents(buttons);

                    await ctx.EditResponseAsync(message);
                }
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Something went wrong"));
            }

        }

        [SlashCommandGroup("Character", "Create or edit characters")]
        public class CharacterGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates a character or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("name", "Character name")] string name, [Option("description", "Character description")] string description, [Option("discord_id", "The Discord ID of whoever can control the character")] string discord_id = null, [Option("id", "The id of the character you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

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
                        embed.AddField(character.name, $"ID: {character.id}\nDescription: {character.description}\nDiscord ID: {character.discord_id}");
                    }

                    pages.Add(new Page("", embed)); // Add the embed as a new page
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Character List")); // Edit the original response
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages); // Send the paginated message
            }

        }

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

        }

        [SlashCommandGroup("Status", "Create or edit statuses")]
        public class StatusGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates a status or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("name", "Status name")] string name, [Option("description", "Status description")] string description, [Option("type", "Item type")] string type = "", [Option("id", "The id of the item you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

                Status newStatus = new Status
                {
                    id = id,
                    name = name,
                    description = description,
                    type = type
                };

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddStatus(ctx.Guild.Id, newStatus);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status added"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("List", "Lists all statuses you have access to")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource); // Send initial response

                var interactivity = ctx.Client.GetInteractivity(); // Get the interactivity module

                var DBEngine = new DBEngine();
                var statuses = await DBEngine.GetStatuses(ctx.Guild.Id);

                var pages = new List<Page>(); // Create a list to hold your pages

                int pageCount = 1;
                for (int i = 0; i < statuses.Count; i += 25) // Loop through characters, 25 at a time
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Page {pageCount++}",
                        Color = DiscordColor.Blue
                    };

                    for (int j = i; j < i + 25 && j < statuses.Count; j++) // Add up to 25 characters to the embed
                    {
                        var status = statuses[j];
                        embed.AddField(status.name, $"ID: {status.id}\nDescription: {status.description}\nType: {status.type}");
                    }

                    pages.Add(new Page("", embed)); // Add the embed as a new page
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status List")); // Edit the original response
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages); // Send the paginated message
            }

        }

        [SlashCommandGroup("Effect", "Create or edit effects")]
        public class EffectGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates an effect or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("effect", "The effect function")] string effect, [Option("id", "The id of the item you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

                Effect newEffect = new Effect
                {
                    id = id,
                    effect = effect
                };

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddEffect(ctx.Guild.Id, newEffect);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Effect added"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("List", "Lists all effects you have access to")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource); // Send initial response

                var interactivity = ctx.Client.GetInteractivity(); // Get the interactivity module

                var DBEngine = new DBEngine();
                var effects = await DBEngine.GetEffects(ctx.Guild.Id);

                var pages = new List<Page>(); // Create a list to hold your pages

                int pageCount = 1;
                for (int i = 0; i < effects.Count; i += 25) // Loop through characters, 25 at a time
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Page {pageCount++}",
                        Color = DiscordColor.Blue
                    };

                    for (int j = i; j < i + 25 && j < effects.Count; j++) // Add up to 25 characters to the embed
                    {
                        var effect = effects[j];
                        embed.AddField($"ID: {effect.id}", $"Effect: {effect.effect}");
                    }

                    pages.Add(new Page("", embed)); // Add the embed as a new page
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Effect List")); // Edit the original response
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages); // Send the paginated message
            }

        }
    }
}