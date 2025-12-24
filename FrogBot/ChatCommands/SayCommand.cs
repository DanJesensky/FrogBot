using System;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
[UsedImplicitly]
public class SayCommand(IDiscordRestChannelAPI channelApi) : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!say");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var messageParts = message.Content.Split(' ', 3,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (messageParts.Length < 3)
        {
            await channelApi.CreateMessageAsync(message.ChannelID, "Wrong format");
        }

        if (!ulong.TryParse(messageParts[1], out var channelId))
        {
            await channelApi.CreateMessageAsync(message.ChannelID, "Channel id is not a long");
            return Result.FromSuccess();
        }

        await channelApi.CreateMessageAsync(new Snowflake(channelId), messageParts[2]);
        return Result.FromSuccess();
    }
}