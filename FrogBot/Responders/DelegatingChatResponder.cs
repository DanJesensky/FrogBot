using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class DelegatingChatResponder : IResponder<IMessageCreate>
{
    private readonly IEnumerable<IChatResponder> _chatResponders;
    private readonly ILogger<DelegatingChatResponder> _logger;
    private readonly IMessageRetriever _messageRetriever;

    public DelegatingChatResponder(IEnumerable<IChatResponder> chatResponders, ILogger<DelegatingChatResponder> logger, IMessageRetriever messageRetriever)
    {
        _chatResponders = chatResponders;
        _logger = logger;
        _messageRetriever = messageRetriever;
    }

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        var message = await _messageRetriever.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, ct);
        if (message == null)
        {
            _logger.LogDebug("Message {messageId} is not a valid reference, ignoring message", gatewayEvent.ID);
            return Result.FromSuccess();
        }

        foreach (var responder in _chatResponders)
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