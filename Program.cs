using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTRPGCreator.Commands;
using TTRPGCreator.Commands.Slash;
using TTRPGCreator.Config;
using TTRPGCreator.Database;
using TTRPGCreator.Events;
using TTRPGCreator.Functionality;

namespace TTRPGCreator
{
    internal class Program
    {
        public static DiscordClient client { get; set; }
        public static CommandsNextExtension commands { get; set; }
        public static SlashCommandsExtension slashCommands { get; set; }

        static async Task Main(string[] args)
        {
            var configJson = new JSONReader();
            await configJson.ReadJSON();

            var discordConfig = new DiscordConfiguration
            {
                Intents = DiscordIntents.All,
                Token = configJson.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            client = new DiscordClient(discordConfig);

            client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            client.Ready += Client_Ready;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { configJson.prefix },
                EnableMentionPrefix = true,
                EnableDms = true
            };
            commands = client.UseCommandsNext(commandsConfig);
            slashCommands = client.UseSlashCommands();

            // Register commands
            commands.RegisterCommands<Commands_System>();
            // commands.RegisterCommands<TestCommands>();
            // slashCommandsConfig.RegisterCommands<TestSlashCommands>();
            slashCommands.RegisterCommands<TestSlashCommands>(1067104957356052501);
            slashCommands.RegisterCommands<SlashCommands_System>(1067104957356052501);

            // Register events
            TestEvents.RegisterEvents();
            //ButtonEvents.RegisterEvents();
            Events_System.RegisterEvents();

            DBEngine dbEngine = new DBEngine();
            await dbEngine.Init();

            await client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
