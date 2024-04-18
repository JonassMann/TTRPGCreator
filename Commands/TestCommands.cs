//using DSharpPlus.CommandsNext;
//using DSharpPlus.CommandsNext.Attributes;
//using DSharpPlus.Entities;
//using DSharpPlus.Interactivity.Extensions;
//using DSharpPlus.SlashCommands;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using TTRPGCreator.Other;
//using TTRPGCreator.Functionality;
//using DSharpPlus;
//using TTRPGCreator.Database;

//namespace TTRPGCreator.Commands
//{
//    public class TestCommands : BaseCommandModule
//    {
//        [Command("ping"), Description("Reply with Pong!")]
//        public async Task Ping(CommandContext ctx)
//        {
//            await ctx.RespondAsync("Pong!");
//        }

//        [Command("hello")]
//        public async Task Hello(CommandContext ctx)
//        {
//            await ctx.Channel.SendMessageAsync($"Hello, {ctx.User.Mention}!");
//        }

//        [Command("add"), Description("Add two numbers")]
//        public async Task Add(CommandContext ctx, [Description("First number")] int numberOne, [Description("Second number")] int numberTwo)
//        {
//            int result = numberOne + numberTwo;
//            await ctx.Channel.SendMessageAsync(result.ToString());
//        }

//        [Command("embed")]
//        public async Task Embed(CommandContext ctx)
//        {
//            var message = new DiscordEmbedBuilder
//            {
//                Title = "This is an embed!",
//                Description = "This is a description!",
//                Color = DiscordColor.Gold,
//                Timestamp = DateTimeOffset.Now
//            };

//            message.AddField("Field 1", "Value 1");
//            message.AddField("Field 2", "Value 2");

//            await ctx.Channel.SendMessageAsync(embed: message);
//        }

//        [Command("cardgame")]
//        public async Task CardGame(CommandContext ctx)
//        {
//            var cardSystem = new CardSystem();

//            bool result = false;
//            while (!result)
//            {
//                result = await CardGameDraw(cardSystem, ctx);
//            }
//        }

//        public async Task<bool> CardGameDraw(CardSystem cardSystem, CommandContext ctx)
//        {
//            Card userCard = cardSystem.GetCard();
//            Card botCard = cardSystem.GetCard();

//            var cardEmbed = new DiscordEmbedBuilder
//            {
//                Title = "Drawing cards!",
//                Color = DiscordColor.Gold
//            };

//            cardEmbed.AddField("Your card", $"{userCard.value} of {userCard.suit}", inline: true);
//            cardEmbed.AddField("Bot card", $"{botCard.value} of {botCard.suit}", inline: true);


//            bool returnValue = false;
//            if (Array.IndexOf(cardSystem.cardValues, userCard.value) > Array.IndexOf(cardSystem.cardValues, botCard.value))
//            {
//                cardEmbed.AddField("Winner", "You!");
//                returnValue = true;
//            }
//            else if (Array.IndexOf(cardSystem.cardValues, userCard.value) < Array.IndexOf(cardSystem.cardValues, botCard.value))
//            {
//                cardEmbed.AddField("Winner", "Bot!");
//                returnValue = true;
//            }
//            else
//            {
//                cardEmbed.AddField("Winner", "Tie!");
//                returnValue = false;
//            }

//            await ctx.Channel.SendMessageAsync(embed: cardEmbed);
//            return returnValue;
//        }

//        [Command("reply")]
//        public async Task Reply(CommandContext ctx)
//        {
//            var interactivity = Program.client.GetInteractivity();

//            var messageToRetrieve = await interactivity.WaitForMessageAsync(message => message.Content == "Hello");
//            if (!messageToRetrieve.TimedOut)
//            {
//                await ctx.Channel.SendMessageAsync("Message recieved");
//            }
//        }

//        [Command("reaction")]
//        public async Task Reaction(CommandContext ctx, ulong messageId = 0)
//        {
//            if (messageId != 0)
//                await ctx.Message.DeleteAsync();

//            var interactivity = Program.client.GetInteractivity();

//            var messageToReact = await interactivity.WaitForReactionAsync(message => message.Message.Id == (messageId == 0 ? ctx.Message.Id : messageId));

//            if (!messageToReact.TimedOut)
//            {
//                await ctx.Channel.SendMessageAsync($"{ctx.User.Username} used the emoji {messageToReact.Result.Emoji}");
//            }
//        }

//        [Command("poll")]
//        public async Task Poll(CommandContext ctx, [RemainingText] string input)
//        {
//            string[] options = input.Split(';');

//            if (options.Length > 11)
//            {
//                await ctx.Channel.SendMessageAsync("Too many options! Max 10");
//                return;
//            }
//            await ctx.Message.DeleteAsync();
//            var pollTime = TimeSpan.FromMinutes(15);

//            var interactivity = Program.client.GetInteractivity();
//            DiscordEmoji[] optionEmojis = { DiscordEmoji.FromName(Program.client, ":one:"),
//                                            DiscordEmoji.FromName(Program.client, ":two:"),
//                                            DiscordEmoji.FromName(Program.client, ":three:"),
//                                            DiscordEmoji.FromName(Program.client, ":four:"),
//                                            DiscordEmoji.FromName(Program.client, ":five:"),
//                                            DiscordEmoji.FromName(Program.client, ":six:"),
//                                            DiscordEmoji.FromName(Program.client, ":seven:"),
//                                            DiscordEmoji.FromName(Program.client, ":eight:"),
//                                            DiscordEmoji.FromName(Program.client, ":nine:"),
//                                            DiscordEmoji.FromName(Program.client, ":keycap_ten:")};

//            string description = "";
//            for (int i = 1; i < options.Length; i++)
//                description += $"{optionEmojis[i - 1]} | {options[i]}\n";
//            description.TrimEnd('\n');

//            var pollEmbed = new DiscordEmbedBuilder
//            {
//                Color = DiscordColor.Gold,
//                Title = options[0],
//                Description = description
//            };

//            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed);

//            for (int i = 1; i < options.Length; i++)
//                await pollMessage.CreateReactionAsync(optionEmojis[i - 1]);

//            int[] count = new int[options.Length - 1];

//            var totalReactions = await interactivity.CollectReactionsAsync(pollMessage, pollTime);
//            foreach (var reaction in totalReactions)
//            {
//                for (int i = 0; i < options.Length - 1; i++)
//                {
//                    if (reaction.Emoji == optionEmojis[i])
//                    {
//                        count[i]++;
//                        break;
//                    }
//                }
//            }

//            int totalvotes = 0;
//            for (int i = 0; i < options.Length - 1; i++)
//                totalvotes += count[i];

//            var resultDescription = "";
//            for (int i = 0; i < options.Length - 1; i++)
//                resultDescription += $"{optionEmojis[i]} | {options[i + 1]} | {count[i]} votes | {Math.Round((double)count[i] / totalvotes * 100, 2)}%\n";

//            var resultEmbed = new DiscordEmbedBuilder
//            {
//                Color = DiscordColor.Gold,
//                Title = "Poll results",
//                Description = resultDescription
//            };

//            await ctx.Channel.SendMessageAsync(embed: resultEmbed);
//        }

//        [Command("cooldown")]
//        [Cooldown(3, 10, CooldownBucketType.Global)]
//        public async Task Cooldown(CommandContext ctx)
//        {
//            await ctx.Channel.SendMessageAsync("Cooldown test command");
//        }

//        [Command("deleteallcommands")]
//        public async Task DeleteAllCommands(CommandContext ctx)
//        {
//            // Fetch all global commands
//            var globalCommands = await ctx.Client.GetGlobalApplicationCommandsAsync();

//            // Delete all global commands
//            foreach (var command in globalCommands)
//            {
//                await ctx.Client.DeleteGlobalApplicationCommandAsync(command.Id);
//            }

//            // Fetch all guild commands
//            var guildCommands = await ctx.Client.GetGuildApplicationCommandsAsync(ctx.Guild.Id);

//            // Delete all guild commands
//            foreach (var command in guildCommands)
//            {
//                await ctx.Client.DeleteGuildApplicationCommandAsync(ctx.Guild.Id, command.Id);
//            }

//            await ctx.Channel.SendMessageAsync("All commands deleted.");
//        }

//        [Command("calc")]
//        public async Task Calc(CommandContext ctx, [RemainingText] string input)
//        {
//            double result = Calculator.Eval(input);
//            await ctx.Channel.SendMessageAsync($"Result: {result}");
//        }

//        [Command("button")]
//        public async Task Button(CommandContext ctx)
//        {
//            var button1 = new DiscordButtonComponent(ButtonStyle.Primary, "button1", "Button 1");
//            var button2 = new DiscordButtonComponent(ButtonStyle.Primary, "button2", "Button 2");

//            var buttonEmbed = new DiscordEmbedBuilder
//            {
//                Title = "Button test",
//                Color = DiscordColor.Gold
//            };
//            var message = new DiscordMessageBuilder().AddEmbed(buttonEmbed).AddComponents(button1, button2);

//            await ctx.Channel.SendMessageAsync(message);
//        }

//        //[Command("help")]
//        //public async Task Help(CommandContext ctx)
//        //{
//        //    var testButton = new DiscordButtonComponent(ButtonStyle.Primary, "testButton", "Test Commands");
//        //    var functionButton = new DiscordButtonComponent(ButtonStyle.Success, "functionButton", "Function Commands");

//        //    var message = new DiscordMessageBuilder().
//        //        AddEmbed(new DiscordEmbedBuilder()
//        //        .WithTitle("Help Section")
//        //        .WithColor(DiscordColor.DarkButNotBlack)
//        //        .WithDescription("Press a button to view its commands"))
//        //    .AddComponents(testButton, functionButton);

//        //    await ctx.Channel.SendMessageAsync(message);
//        //}

//        [Command("dropdown")]
//        public async Task Dropdown(CommandContext ctx)
//        {
//            List<DiscordSelectComponentOption> options = new List<DiscordSelectComponentOption>
//            {
//                new DiscordSelectComponentOption("Option 1", "option1"),
//                new DiscordSelectComponentOption("Option 2", "option2"),
//                new DiscordSelectComponentOption("Option 3", "option3")
//            };

//            var dropDown = new DiscordSelectComponent("dropdown", "Select...", options);

//            var dropDownMessage = new DiscordMessageBuilder()
//                .AddEmbed(new DiscordEmbedBuilder()
//                .WithColor(DiscordColor.Gold)
//                .WithTitle("Dropdown List"))
//            .AddComponents(dropDown);

//            await ctx.Channel.SendMessageAsync(dropDownMessage);
//        }

//        [Command("channellist")]
//        public async Task ChannelList(CommandContext ctx)
//        {
//            var channelComponent = new DiscordChannelSelectComponent("channellist", "Select a channel");

//            var dropDownMessage = new DiscordMessageBuilder()
//                .AddEmbed(new DiscordEmbedBuilder()
//                .WithColor(DiscordColor.Gold)
//                .WithTitle("Channel List"))
//            .AddComponents(channelComponent);

//            await ctx.Channel.SendMessageAsync(dropDownMessage);
//        }

//        [Command("mentionlist")]
//        public async Task MentionList(CommandContext ctx)
//        {
//            var mentionComponent = new DiscordMentionableSelectComponent("mentionlist", "Select a user");

//            var dropDownMessage = new DiscordMessageBuilder()
//                .AddEmbed(new DiscordEmbedBuilder()
//                .WithColor(DiscordColor.Gold)
//                .WithTitle("Mention List"))
//            .AddComponents(mentionComponent);

//            await ctx.Channel.SendMessageAsync(dropDownMessage);
//        }

//        [Command("store")]
//        public async Task Store(CommandContext ctx)
//        {
//            var user = new DUser
//            {
//                userName = ctx.User.Username,
//                serverName = ctx.Guild.Name,
//                serverID = ctx.Guild.Id
//            };

//            var dbEngine = new DBEngine();
//            var isStored = await dbEngine.StoreUserAsync(user);

//            if (isStored)
//                await ctx.Channel.SendMessageAsync("User stored successfully");
//            else
//                await ctx.Channel.SendMessageAsync("User could not be stored");
//        }

//        [Command("profile")]
//        public async Task Profile(CommandContext ctx)
//        {
//            var DBEngine = new DBEngine();

//            var userToRetrieve = await DBEngine.GetUserAsync(ctx.User.Username, ctx.Guild.Id);
//            if (userToRetrieve.Item1)
//            {
//                var profileEmbed = new DiscordEmbedBuilder
//                {
//                    Title = $"{userToRetrieve.Item2.userName}'s profile",
//                    Color = DiscordColor.Gold,
//                    Description = $"Server name : {userToRetrieve.Item2.serverName}\n" +
//                                  $"Server ID : {userToRetrieve.Item2.serverID}"
//                };

//                await ctx.Channel.SendMessageAsync(embed: profileEmbed);
//            }
//            else
//                await ctx.Channel.SendMessageAsync("Something went wrong");
//        }
//    }
//}
