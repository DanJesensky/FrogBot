using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteAddResponder : IResponder<IMessageReactionAdd>
{
    private readonly IOptions<FrogBotOptions> _botOptions;
    private readonly IVoteManager _voteManager;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IVoteEmojiProvider _voteEmojiProvider;

    public VoteAddResponder(IOptions<FrogBotOptions> botOptions, IVoteManager voteManager, IDiscordRestChannelAPI channelApi, IVoteEmojiProvider voteEmojiProvider)
    {
        _botOptions = botOptions;
        _voteManager = voteManager;
        _channelApi = channelApi;
        _voteEmojiProvider = voteEmojiProvider;
    }

    public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
    {
        var user = gatewayEvent.Member.Value.User.Value;

        // Bots can't vote.
        if (user.IsBot.HasValue && user.IsBot.Value)
        {
            return Result.FromSuccess();
        }

        // If the vote type is null, the reaction was an emoji that isn't supported.
        var voteType = _voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
        if (voteType is null)
        {
            return Result.FromSuccess();
        }

        // Fetch message details from Discord
        var messageResult = await _channelApi.GetChannelMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (!messageResult.IsSuccess)
        {
            await _voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError(messageResult.Error);
        }

        // Users can't vote on bots.
        var author = messageResult.Entity.Author;
        if (author.IsBot.HasValue && author.IsBot.Value)
        {
            return Result.FromSuccess();
        }

        // Can't vote on self
        if (author.ID.Value == gatewayEvent.UserID.Value)
        {
            return Result.FromSuccess();
        }

        // Locked to this server for now
        if (!gatewayEvent.GuildID.HasValue || gatewayEvent.GuildID.Value.Value != _botOptions.Value.ServerId)
        {
            return Result.FromSuccess();
        }

        await _voteManager.AddVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}