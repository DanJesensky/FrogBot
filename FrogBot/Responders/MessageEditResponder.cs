using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

[UsedImplicitly]
public class MessageEditResponder(ITikTokQuarantineResponder quarantine, IDiscordRestChannelAPI channel)
    : IResponder<IMessageUpdate>
{
    public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = default)
    {
        IMessage? message;
        if (gatewayEvent.ReferencedMessage.HasValue && gatewayEvent.ReferencedMessage.Value != null)
        {
            message = gatewayEvent.ReferencedMessage.Value;
        }
        else
        {
            var messageFetch = await channel.GetChannelMessageAsync(gatewayEvent.ChannelID.Value, gatewayEvent.ID.Value, ct);
            if (messageFetch is { IsSuccess: false })
            {
                return Result.FromError<string>("Message does not exist.");
            }

            message = messageFetch.Entity;
        }

        return await quarantine.RespondAsync(message, ct);
    }
}