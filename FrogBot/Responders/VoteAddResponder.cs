using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteAddResponder : IResponder<IMessageReactionAdd>
{
    private readonly ILogger<VoteAddResponder> _logger;
    private readonly IOptions<FrogBotOptions> _botOptions;
    private readonly IVoteManager _voteManager;
    private readonly IMessageRetriever _messageRetriever;
    private readonly IVoteEmojiProvider _voteEmojiProvider;
    private readonly ITikTokQuarantineManager _quarantine;

    public VoteAddResponder(ILogger<VoteAddResponder> logger, IOptions<FrogBotOptions> botOptions, IVoteManager voteManager, IMessageRetriever messageRetriever, IVoteEmojiProvider voteEmojiProvider, ITikTokQuarantineManager quarantine)
    {
        _logger = logger;
        _botOptions = botOptions;
        _voteManager = voteManager;
        _messageRetriever = messageRetriever;
        _voteEmojiProvider = voteEmojiProvider;
        _quarantine = quarantine;
    }

    public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
    {
        var user = gatewayEvent.Member.Value.User.Value;

        // Bots can't vote.
        if (user.IsBot.HasValue && user.IsBot.Value)
        {
            _logger.LogDebug("Bot vote on message {messageId} ignored.", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // If the vote type is null, the reaction was an emoji that isn't supported.
        var voteType = _voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
        if (voteType is null)
        {
            _logger.LogDebug("Reaction on message {messageId} was not a valid vote emoji, ignoring.", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Fetch message details from Discord
        var message = await _messageRetriever.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (message == null)
        {
            _logger.LogError("Failed to record vote type {voteType} for message {messageId}: the message does not exist.", voteType, gatewayEvent.MessageID);
            await _voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError<string>("Message does not exist.");
        }

        // Users can't vote on bots, except for in the TikTok quarantine.
        // In that case, the vote is attributed to the original author whose message was deleted for quarantine.
        var author = _quarantine.GetSubstituteQuarantineAuthor(message);
        if (author.IsBot.HasValue && author.IsBot.Value)
        {
            _logger.LogDebug("Ignoring user vote on bot message {messageId}", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Can't vote on self
        if (author.ID.Value == gatewayEvent.UserID.Value)
        {
            _logger.LogDebug("Ignoring user vote on self for message {messageId}", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Locked to servers
        if (!gatewayEvent.GuildID.HasValue)
        {
            _logger.LogDebug("Ignoring vote for direct message.");
            return Result.FromSuccess();
        }

        // Only the configured server is eligible
        if (gatewayEvent.GuildID.Value.Value != _botOptions.Value.ServerId)
        {
            _logger.LogError("Ignoring vote for disallowed guild {guildId}", gatewayEvent.GuildID.Value);
            return Result.FromSuccess();
        }

        await _voteManager.AddVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}