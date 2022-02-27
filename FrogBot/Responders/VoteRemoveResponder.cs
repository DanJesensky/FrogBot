using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using FrogBot.Voting;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteRemoveResponder : IResponder<IMessageReactionRemove>
{
    private readonly IVoteManager _voteManager;
    private readonly IMessageRetriever _channelApi;
    private readonly IVoteEmojiProvider _voteEmojiProvider;
    private readonly ITikTokQuarantineManager _quarantine;

    public VoteRemoveResponder(IVoteManager voteManager, IMessageRetriever channelApi, IVoteEmojiProvider voteEmojiProvider, ITikTokQuarantineManager quarantine)
    {
        _voteManager = voteManager;
        _channelApi = channelApi;
        _voteEmojiProvider = voteEmojiProvider;
        _quarantine = quarantine;
    }

    public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
    {
        var voteType = _voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
        if (voteType is null)
        {
            return Result.FromSuccess();
        }

        var message = await _channelApi.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (message == null)
        {
            await _voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError<string>("Message does not exist.");
        }

        var author = _quarantine.GetSubstituteQuarantineAuthor(message);

        await _voteManager.RemoveVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}