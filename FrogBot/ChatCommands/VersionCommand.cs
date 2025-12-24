using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[UsedImplicitly]
public class VersionCommand(IDiscordRestChannelAPI channelApi) : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.Equals("!version");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        await channelApi.CreateMessageAsync(message.ChannelID, typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "Version unset");
        return Result.FromSuccess();
    }
}