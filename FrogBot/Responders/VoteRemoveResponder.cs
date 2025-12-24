using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using FrogBot.Voting;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteRemoveResponder(
    IVoteManager voteManager,
    IMessageRetriever channelApi,
    IVoteEmojiProvider voteEmojiProvider,
    ITikTokQuarantineManager quarantine)
    : IResponder<IMessageReactionRemove>
{
    public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
    {
        var voteType = voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
        if (voteType is null)
        {
            return Result.FromSuccess();
        }

        var message = await channelApi.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (message == null)
        {
            await voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError<string>("Message does not exist.");
        }

        var author = quarantine.GetSubstituteQuarantineAuthor(message);

        await voteManager.RemoveVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}