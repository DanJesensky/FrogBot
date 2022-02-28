using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteRemoveResponder : IResponder<IMessageReactionRemove>
{
    private readonly IVoteManager _voteManager;
    private readonly IMessageRetriever _channelApi;
    private readonly IVoteEmojiProvider _voteEmojiProvider;

    public VoteRemoveResponder(IVoteManager voteManager, IMessageRetriever channelApi, IVoteEmojiProvider voteEmojiProvider)
    {
        _voteManager = voteManager;
        _channelApi = channelApi;
        _voteEmojiProvider = voteEmojiProvider;
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

        await _voteManager.RemoveVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, message.Author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}