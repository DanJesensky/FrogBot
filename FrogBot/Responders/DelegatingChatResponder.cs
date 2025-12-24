using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

[UsedImplicitly]
public class DelegatingChatResponder(
    IEnumerable<IChatResponder> chatResponders,
    ILogger<DelegatingChatResponder> logger,
    IMessageRetriever messageRetriever)
    : IResponder<IMessageCreate>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        var message = await messageRetriever.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, ct);
        if (message == null)
        {
            logger.LogDebug("Message {messageId} is not a valid reference, ignoring message", gatewayEvent.ID);
            return Result.FromSuccess();
        }

        foreach (var responder in chatResponders)
        {
            var result = await responder.RespondAsync(message, ct);
            if (result is not { IsSuccess: true })
            {
                return result;
            }
        }
        
        return Result.FromSuccess();
    }
}