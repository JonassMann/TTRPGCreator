using DSharpPlus.CommandsNext;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Runtime.CompilerServices;
using TTRPGCreator.System;
using TTRPGCreator.Database;

namespace TTRPGCreator.Events
{
    public class Events_System
    {
        public static void RegisterEvents()
        {
            Program.client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Program.client.ModalSubmitted += Client_ModalSubmitted;
        }

        private static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            switch (e.Interaction.Data.CustomId)
            {
                case string s when s.Contains("system_gameChange"):

                    string gameName = s.Remove(0, "system_gameChange".Length);
                    var DBEngine = new DBEngine();

                    bool querySuccess = await DBEngine.SetGame(e.Interaction.Guild.Id, gameName);
                    if (querySuccess)
                    {
                        DataCache.gameList[e.Interaction.Guild.Id] = $"\"{e.Interaction.Guild.Id}_{gameName}\"";
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Successfully changed game to {gameName}"));
                    }
                    else
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Something went wrong"));
                    }
                    break;

                default:
                    break;
            }
        }

        private static async Task Client_ModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
        {
            switch (e.Interaction.Data.CustomId)
            {
                case "system_gameCreate":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                    var DBEngine = new DBEngine();

                    Ruleset ruleSet = new Ruleset();
                    //string[] dice = e.Values["defaultDice"].Split('d');
                    //if (dice.Length > 1 && int.TryParse(dice[0], out int diceNum) && int.TryParse(dice[1], out int diceSize))
                    //{
                    //    ruleSet.diceNum = diceNum;
                    //    ruleSet.diceSize = diceSize;
                    //    if(dice.Length > 2 && int.TryParse(dice[2], out int diceMod))
                    //        ruleSet.diceMod = diceMod;
                    //}
                    //else
                    //{
                    //    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Invalid dice format"));
                    //}

                    ruleSet.diceRoll = e.Values["defaultDice"];
                    ruleSet.statFormula = e.Values["statFormula"];

                    string[] stats = e.Values["characterStats"].Split('|');
                    foreach (string stat in stats)
                    {
                        string[] statParts = stat.Split(';');
                        if (statParts.Length == 2)
                            ruleSet.stats.Add((statParts[0], statParts[1]));
                    }

                    bool querySuccess = await DBEngine.CreateGame(e.Interaction.Guild.Id, e.Values["gameName"], ruleSet);

                    if (querySuccess)
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Game created"));
                    else
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));

                    break;

                default:
                    break;
            }
        }
    }
}
