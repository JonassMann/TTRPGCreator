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
using System.Diagnostics;
using System;

namespace TTRPGCreator.Commands
{
    public class SlashCommands_Status : ApplicationCommandModule
    {
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

            [SlashCommand("Status", "Edits status information for a status")]
            public async Task Status(InteractionContext ctx, [Option("parent_status", "Parent status id")] long parent_status, [Option("child_status", "Child status id")] long child_status, [Option("level", "The level of the status")] double level = 1, [Option("delete", "Use true to delete status from status")] bool delete = false)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                int querySuccess = await DBEngine.AddStatusStatus(ctx.Guild.Id, parent_status, child_status, (int)level, delete);

                if (querySuccess == 1)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status added to status"));
                else if (querySuccess == 2)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status removed from status"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Effect", "Edits effect information for a status")]
            public async Task Status(InteractionContext ctx, [Option("status", "Status id")] long status, [Option("effect", "Effect id")] long effect, [Option("level", "The level of the effect")] long level = 1, [Option("delete", "Use true to delete effect from status")] bool delete = false)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                int querySuccess = await DBEngine.AddStatusEffect(ctx.Guild.Id, status, effect, level, delete);

                if (querySuccess == 1)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Effect added to status"));
                else if (querySuccess == 2)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Effect removed from status"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Get", "Get information of a status")]
            public async Task Get(InteractionContext ctx, [Option("status", "Status id")] long statusId)
            {
                // Defer the reply. This is especially useful for longer running commands
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();

                var statusInfo = await DBEngine.GetStatus(ctx.Guild.Id, statusId, true);
                if (!statusInfo.Item1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
                    return;
                }

                Status statusValues = statusInfo.Item2;

                // Create a new embed builder
                var embed = new DiscordEmbedBuilder
                {
                    Title = statusValues.name,
                    Description = statusValues.description,
                    Color = DiscordColor.Blurple // You can set the embed color here
                };

                if (statusValues.statuses != null)
                    foreach (Status status in statusValues.statuses)
                        embed.AddField("Status: " + status.name, $"ID: {status.id}\nDescription: {status.description}\nType: {status.type}");

                if (statusValues.effects != null)
                    foreach (Effect effect in statusValues.effects)
                    {
                        string tagString = "None";
                        if (effect.tags != null && effect.tags.Count > 0)
                        {
                            tagString = "";
                            foreach (string tag in effect.tags)
                                tagString += $"{tag}, ";

                            tagString = tagString.Remove(tagString.Length - 2);
                        }
                        embed.AddField("Effect:", $"ID: {effect.id}\nEffect: {effect.effect}\nLevel: {effect.level}\nTags: {tagString}");
                    }

                // Edit the original deferred message with the new embed
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.Build()));
            }

            [SlashCommand("Delete", "Deletes a status")]
            public async Task Delete(InteractionContext ctx, [Option("status", "Status id")] long status)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.DeleteStatus(ctx.Guild.Id, status);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Status deleted"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

        }

        [SlashCommandGroup("Effect", "Create or edit effects")]
        public class EffectGroup : ApplicationCommandModule
        {
            [SlashCommand("Create", "Creates an effect or edits it's basic information")]
            public async Task Create(InteractionContext ctx, [Option("effect", "The effect function")] string effect, [Option("tags", "Tags, comma separated")] string tagString = null, [Option("id", "The id of the item you want to edit")] long? id = null)
            {
                await ctx.DeferAsync();

                Effect newEffect = new Effect
                {
                    id = id,
                    effect = effect
                };

                List<string> tags = new List<string>();
                if (tagString != null)
                {
                    var tagArray = tagString.Split(',');
                    foreach (string tag in tagArray)
                        tags.Add(tag);
                }

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddEffect(ctx.Guild.Id, newEffect, tags);

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

            [SlashCommand("Tags", "Edits tag information for an effect")]
            public async Task Status(InteractionContext ctx, [Option("effect", "Effect id")] long status, [Option("tags", "Tags, comma separated")] string tagString, [Option("clear", "Set to true to reset tags")] bool clear = false)
            {
                await ctx.DeferAsync();

                List<string> tags = new List<string>();
                var tagArray = tagString.Split(',');
                foreach (string tag in tagArray)
                    tags.Add(tag);

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.AddEffectTags(ctx.Guild.Id, status, tags, clear);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Tags added to effect"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

            [SlashCommand("Get", "Get information of an effect")]
            public async Task Get(InteractionContext ctx, [Option("effect", "Effect id")] long effectId)
            {
                // Defer the reply. This is especially useful for longer running commands
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();

                var effectInfo = await DBEngine.GetEffect(ctx.Guild.Id, effectId, true);
                if (!effectInfo.Item1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
                    return;
                }

                Effect effect = effectInfo.Item2;
                Console.WriteLine(effect.effect);

                // Create a new embed builder
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Effect id: " + effectId,
                    Description = effect.effect,
                    Color = DiscordColor.Blurple // You can set the embed color here
                };
                Console.WriteLine(effect.tags.Count);

                string tagString = "";
                if (effect.tags != null && effect.tags.Count > 0)
                {
                    foreach (string tag in effect.tags)
                        tagString += $"{tag}, ";

                    tagString = tagString.Remove(tagString.Length - 2);
                }
                embed.AddField("Tags:", tagString == "" ? "None" : tagString);

                // Edit the original deferred message with the new embed
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.Build()));
            }

            [SlashCommand("Delete", "Deletes an effect")]
            public async Task Delete(InteractionContext ctx, [Option("effect", "Effect id")] long effect)
            {
                await ctx.DeferAsync();

                var DBEngine = new DBEngine();
                bool querySuccess = await DBEngine.DeleteEffect(ctx.Guild.Id, effect);

                if (querySuccess)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Effect deleted"));
                else
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            }

        }

        [SlashCommand("GetEffects", "Gets all effects of character with selected tags")]
        public async Task GetEffects(InteractionContext ctx, [Option("tags", "Tags, comma separated")] string tagString, [Option("character", "The character to get effects from")] long? characterId = null)
        {
            await ctx.DeferAsync();

            List<string> tags = new List<string>();
            var tagArray = tagString.Split(',');
            foreach (string tag in tagArray)
                tags.Add(tag);

            var DBEngine = new DBEngine();
            if (characterId == null)
                characterId = (long?)await DBEngine.GetCharacterDiscord(ctx.Guild.Id, ctx.User.Id);

            var effects = await DBEngine.GetAllEffects(ctx.Guild.Id, (long)characterId, tags);

            if (effects == null)
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong"));
            else
            {
                if (effects.Count == 0)
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No effects with tags"));
                else
                {
                    string effectString = "";

                    foreach (string effect in effects)
                    {
                        effectString += $"{effect}\n";
                    }

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(effectString));
                }
            }
        }
    }
}