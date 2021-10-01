using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders
{
    public class DeleteMessageResponder : IResponder<IMessageDelete>
    {
        private readonly IVoteManager _voteManager;

        public DeleteMessageResponder(IVoteManager voteManager)
        {
            _voteManager = voteManager;
        }

        public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = default)
        {
            await _voteManager.RemoveVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.ID.Value, VoteType.Upvote);
            return Result.FromSuccess();
        }
    }
}