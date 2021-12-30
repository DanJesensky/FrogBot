using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using Remora.Discord.API.Abstractions.Objects;
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

    public bool CanHandleCommand(IMessage message) =>
        message.Content.Equals("!test");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        await _channelApi.CreateReactionAsync(message.ChannelID, message.ID, "👍");
        return Result.FromSuccess();
    }
}