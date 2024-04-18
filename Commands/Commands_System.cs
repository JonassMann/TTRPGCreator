using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TTRPGCreator.Other;
using TTRPGCreator.Functionality;
using DSharpPlus;
using TTRPGCreator.Database;
using TTRPGCreator.System;

namespace TTRPGCreator.Commands
{
    public class Commands_System : BaseCommandModule
    {
        [Command("RunSQL"), RequireOwner, Hidden]
        public async Task RunSQL(CommandContext ctx, [RemainingText] string query)
        {
            var DBEngine = new DBEngine();

            var doQuery = await DBEngine.RunSQL(query);
            if (doQuery)
                await ctx.Channel.SendMessageAsync("Query run successfully");
            else
                await ctx.Channel.SendMessageAsync("Something went wrong");
        }
    }
}
