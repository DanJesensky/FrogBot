using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
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

    public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
        messageCreateEvent.Content.Equals("!version");

    public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
    {
        await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "Version unset");
        return Result.FromSuccess();
    }
}