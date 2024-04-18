using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTRPGCreator.Events
{
    internal class TestEvents
    {
        public static void RegisterEvents()
        {
            Program.client.VoiceStateUpdated += Client_VoiceStateUpdated;
            Program.commands.CommandErrored += Commands_CommandErrored;
        }

        private static async Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            if (e.Before == null)
            {
                await e.Channel.SendMessageAsync($"{e.User.Username} joined {e.Channel.Name}");
            }
        }

        private static async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if(e.Exception is ChecksFailedException exception)
            {
                string timeLeft = "";
                foreach(var check in exception.FailedChecks)
                {
                    var coolDown = (CooldownAttribute)check;
                    timeLeft = coolDown.GetRemainingCooldown(e.Context).ToString(@"hh\:mm\:ss");
                }

                var coolDownMessage = new DiscordEmbedBuilder
                {
                    Title = "Cooldown",
                    Description = $"{e.Command.Name} is on cooldown for {timeLeft}",
                    Color = DiscordColor.Red
                };

                await e.Context.Channel.SendMessageAsync(embed: coolDownMessage);
            }
        }
    }
}
