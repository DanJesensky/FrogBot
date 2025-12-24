using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
[UsedImplicitly]
public class TestChatCommand(IDiscordRestChannelAPI channelApi) : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.Equals("!test");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        await channelApi.CreateReactionAsync(message.ChannelID, message.ID, "✅");
        return Result.FromSuccess();
    }
}