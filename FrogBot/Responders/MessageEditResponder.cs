using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class MessageEditResponder : IResponder<IMessageUpdate>
{
    private readonly ITikTokQuarantine _quarantine;
    private readonly IMessageRetriever _channel;

    public MessageEditResponder(ITikTokQuarantine quarantine, IMessageRetriever channel)
    {
        _quarantine = quarantine;
        _channel = channel;
    }

    public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = default)
    {
        IMessage? message;
        if (gatewayEvent.ReferencedMessage.HasValue && gatewayEvent.ReferencedMessage.Value != null)
        {
            message = gatewayEvent.ReferencedMessage.Value;
        }
        else
        {
            message = await _channel.RetrieveMessageAsync(gatewayEvent.ChannelID.Value, gatewayEvent.ID.Value, ct);
            if (message == null)
            {
                return Result.FromError<string>("Message does not exist.");
            }
        }

        return await _quarantine.RespondAsync(message, ct);
    }
}