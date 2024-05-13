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

        [SlashCommand("Calc", "Calculate string expression")]
        public async Task Calc(InteractionContext ctx, [Option("expression", "The string expression")] string expression)
        {
            await ctx.DeferAsync();
            DBEngine DBEngine = new DBEngine();

            var result = await Calculator.Eval(expression, ctx.Guild.Id, (long)await DBEngine.GetCharacterDiscord(ctx.Guild.Id, ctx.User.Id));

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(result.ToString()));
        }
    }
}