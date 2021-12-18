using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
public class TestChatCommand : IChatCommand
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public TestChatCommand(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
        messageCreateEvent.Content.Equals("!test");

    public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
    {
        await _channelApi.CreateReactionAsync(messageCreateEvent.ChannelID, messageCreateEvent.ID, "👍");
        return Result.FromSuccess();
    }
}