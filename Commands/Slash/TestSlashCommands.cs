using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using TTRPGCreator.Functionality;

namespace TTRPGCreator.Commands.Slash
{
    public class TestSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("test", "Test slash command")]
        public async Task Test(InteractionContext ctx)
        {
            // await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Test command response"));
            await ctx.DeferAsync();

            var embedMessage = new DiscordEmbedBuilder
            {
                Title = "Test embed",
                Color = DiscordColor.Gold,
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedMessage));

            // await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Test command response"));
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Test message context")]
        public async Task TestMessageContext(ContextMenuContext ctx)
        {
            await ctx.DeferAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message context thing"));
        }

        [SlashCommand("parameters", "Test slash command for parameters")]
        public async Task Paramaters(InteractionContext ctx, [Option("testOption", "Type anything")] string testParameter, [Option("numberOption", "Type a number")] long num)
        {
            await ctx.DeferAsync();

            var embedMessage = new DiscordEmbedBuilder
            {
                Title = "Test embed",
                Color = DiscordColor.Gold,
                Description = $"{testParameter} | {num}"
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedMessage));
        }

        //[SlashCommand("attachement", "Repost discord attachements")]
        //public async Task Attachements(InteractionContext ctx, [Option("user", "Pass in a discord user")] DiscordUser user, [Option("Image", "Reaction image")] DiscordAttachment file, [Option("Enum test", "Pass in an enum value")] TestEnum testEnum)
        //{
        //    await ctx.DeferAsync();

        //    var embedMessage = new DiscordEmbedBuilder
        //    {
        //        Title = "Test embed",
        //        Color = DiscordColor.Gold,
        //        Description = $"{user.Mention} | {file.Url} | {testEnum}"
        //    };

        //    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedMessage));
        //}

        [SlashCommand("roll", "Roll some dice")]
        public async Task Attachements(InteractionContext ctx, [Option("roll", "The dice to roll, [1d20]")] string diceText)
        {
            await ctx.DeferAsync();

            int? diceRoll = DiceRoller.Roll(diceText);
            string rollMessage = diceRoll == null ?
                                 "Wrong roll syntax" :
                                 $"Rolling {diceText}\nRolled {diceRoll}!";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(rollMessage));
        }

        [SlashCommand("modal", "Show a modal")]
        public async Task Modal(InteractionContext ctx)
        {
            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Test Modal")
                .WithCustomId("modal")
                .AddComponents(new TextInputComponent("Random", "randomTextBox", "Type something here"));

            await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
        }
    }
}
public enum TestEnum
{
    [ChoiceName("Option 1")]
    Test1,
    [ChoiceName("Option 2")]
    Test2,
    [ChoiceName("Option 3")]
    Test3
}