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
    private readonly ILogger<VoteAddResponder> _logger;
    private readonly IOptions<FrogBotOptions> _botOptions;
    private readonly IVoteManager _voteManager;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IVoteEmojiProvider _voteEmojiProvider;

    public VoteAddResponder(ILogger<VoteAddResponder> logger, IOptions<FrogBotOptions> botOptions, IVoteManager voteManager, IDiscordRestChannelAPI channelApi, IVoteEmojiProvider voteEmojiProvider)
    {
        _logger = logger;
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
        var messageResult = await _channelApi.GetChannelMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (!messageResult.IsSuccess)
        {
            _logger.LogError("Failed to record vote type {voteType} for message {messageId}: the message does not exist.", voteType, gatewayEvent.MessageID);
            await _voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError(messageResult.Error);
        }

        // Users can't vote on bots.
        var author = messageResult.Entity.Author;
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