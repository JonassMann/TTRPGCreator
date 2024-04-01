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

namespace TTRPGCreator.Events
{
    public class ButtonEvents
    {
        private static DiscordClient _client;

        public static void RegisterEvents(DiscordClient client)
        {
            _client = client;
            client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            client.ModalSubmitted += Client_ModalSubmitted;
        }

        private static async Task Client_ModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
        {
            if(e.Interaction.Type == InteractionType.ModalSubmit)
            {
                var values = e.Values;
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.Interaction.User.Username} submitted a modal with input: {values.Values.First()}"));
            }
        }

        private static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            switch (e.Interaction.Data.CustomId)
            {
                case "button1":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.User.Username} has pressed button 1"));
                    break;

                case "button2":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.User.Username} has pressed button 2"));
                    break;

                case "testButton":
                    await e.Interaction.DeferAsync();
                    var testCommandEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Test commands",
                        Description = "ping -> Replies with pong\n" +
                        "hello -> Mentions the user that triggered the command\n" +
                        "add -> Adds two numbers\n" +
                        "embed -> Sends a test embed\n" +
                        "reply -> Waits for a message that says Hello\n" +
                        "reaction -> Waits for a reaction\n" +
                        "cooldown -> Cooldown test command\n" +
                        "button -> Button test command",
                        Color = DiscordColor.Gold
                    };
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(testCommandEmbed));
                    break;

                case "functionButton":
                    //await args.Interaction.DeferAsync();
                    var functionCommandEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Function commands",
                        Description = "cardgame -> Plays a simple card game of drawing the highest card\n" +
                        "poll -> Creates a poll with up to 10 options, separated by a ;\n" +
                        "deleteallcommands -> Deletes all slash commands\n" +
                        "calc -> Runs a string based calculator\n" +
                        "help -> Runs the help command\n",
                        Color = DiscordColor.Gold
                    };
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(functionCommandEmbed));
                    //await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(functionCommandEmbed));
                    break;

                case "dropdown":
                    if (e.Interaction.Data.ComponentType != ComponentType.StringSelect)
                        return;

                    var options = e.Interaction.Data.Values;

                    foreach (var option in options)
                    {
                        switch (option)
                        {
                            case "option1":
                                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Option 1 selected"));
                                break;

                            case "option2":
                                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Option 2 selected"));
                                break;

                            case "option3":
                                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Option 3 selected"));
                                break;
                        }
                    }

                    break;

                case "channellist":
                    if (e.Interaction.Data.ComponentType != ComponentType.ChannelSelect)
                        return;

                    var channelOptions = e.Interaction.Data.Values;

                    foreach (var channel in channelOptions)
                    {
                        var selectedChannel = e.Guild.Channels[ulong.Parse(channel)];
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Channel selected"));
                        await selectedChannel.SendMessageAsync(e.User.Mention);
                    }

                    break;

                case "mentionlist":
                    if (e.Interaction.Data.ComponentType != ComponentType.MentionableSelect)
                        return;

                    var mentionOptions = e.Interaction.Data.Values;

                    foreach (var user in mentionOptions)
                    {
                        var selectedUser = await _client.GetUserAsync(ulong.Parse(user));
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(selectedUser.Mention));
                    }

                    break;

                default:
                    break;
            }
        }


    }
}
