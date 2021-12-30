using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

public class VersionCommand : IChatCommand
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public VersionCommand(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    public bool CanHandleCommand(IMessage message) =>
        message.Content.Equals("!version");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        await _channelApi.CreateMessageAsync(message.ChannelID, typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "Version unset");
        return Result.FromSuccess();
    }
}