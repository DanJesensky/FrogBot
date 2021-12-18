using System;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    [BotAdminAuthorization]
    public class SayCommand : IChatCommand
    {
        private readonly IDiscordRestChannelAPI _channelApi;

        public SayCommand(IDiscordRestChannelAPI channelApi)
        {
            _channelApi = channelApi;
        }

        public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
            messageCreateEvent.Content.StartsWith("!say");

        public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
        {
            var messageParts = messageCreateEvent.Content.Split(' ', 3,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length < 3)
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, "Wrong format");
            }

            if (!ulong.TryParse(messageParts[1], out var channelId))
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, "Channel id is not a long");
                return Result.FromSuccess();
            }

            await _channelApi.CreateMessageAsync(new Snowflake(channelId), messageParts[2]);
            return Result.FromSuccess();
        }
    }
}