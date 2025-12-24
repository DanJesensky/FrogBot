using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

[UsedImplicitly]
public class DeleteMessageResponder(IVoteManager voteManager) : IResponder<IMessageDelete>
{
    public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = default)
    {
        await voteManager.RemoveVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.ID.Value, VoteType.Upvote);
        return Result.FromSuccess();
    }
}