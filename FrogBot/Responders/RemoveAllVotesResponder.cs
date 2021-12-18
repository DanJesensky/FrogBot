using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class RemoveAllVotesResponder : IResponder<IMessageReactionRemoveAll>
{
    public Task<Result> RespondAsync(IMessageReactionRemoveAll gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        throw new System.NotImplementedException();
    }
}