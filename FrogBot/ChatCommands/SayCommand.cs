using System;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
public class SayCommand : IChatCommand
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public SayCommand(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!say");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var messageParts = message.Content.Split(' ', 3,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (messageParts.Length < 3)
        {
            await _channelApi.CreateMessageAsync(message.ChannelID, "Wrong format");
        }

        if (!ulong.TryParse(messageParts[1], out var channelId))
        {
            await _channelApi.CreateMessageAsync(message.ChannelID, "Channel id is not a long");
            return Result.FromSuccess();
        }

        await _channelApi.CreateMessageAsync(new Snowflake(channelId), messageParts[2]);
        return Result.FromSuccess();
    }
}