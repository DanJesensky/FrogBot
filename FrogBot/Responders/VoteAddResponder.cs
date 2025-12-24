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

public class VoteAddResponder(
    ILogger<VoteAddResponder> logger,
    IOptions<FrogBotOptions> botOptions,
    IVoteManager voteManager,
    IMessageRetriever messageRetriever,
    IVoteEmojiProvider voteEmojiProvider,
    ITikTokQuarantineManager quarantine)
    : IResponder<IMessageReactionAdd>
{
    public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
    {
        var user = gatewayEvent.Member.Value.User.Value;

        // Bots can't vote.
        if (user.IsBot.HasValue && user.IsBot.Value)
        {
            logger.LogDebug("Bot vote on message {messageId} ignored.", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // If the vote type is null, the reaction was an emoji that isn't supported.
        var voteType = voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
        if (voteType is null)
        {
            logger.LogDebug("Reaction on message {messageId} was not a valid vote emoji, ignoring.", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Fetch message details from Discord
        var message = await messageRetriever.RetrieveMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
        if (message == null)
        {
            logger.LogError("Failed to record vote type {voteType} for message {messageId}: the message does not exist.", voteType, gatewayEvent.MessageID);
            await voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
            return Result.FromError<string>("Message does not exist.");
        }

        // Users can't vote on bots, except for in the TikTok quarantine.
        // In that case, the vote is attributed to the original author whose message was deleted for quarantine.
        var author = quarantine.GetSubstituteQuarantineAuthor(message);
        if (author.IsBot.HasValue && author.IsBot.Value)
        {
            logger.LogDebug("Ignoring user vote on bot message {messageId}", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Can't vote on self
        if (author.ID.Value == gatewayEvent.UserID.Value)
        {
            logger.LogDebug("Ignoring user vote on self for message {messageId}", gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        // Locked to servers
        if (!gatewayEvent.GuildID.HasValue)
        {
            logger.LogDebug("Ignoring vote for direct message.");
            return Result.FromSuccess();
        }

        // Only the configured server is eligible
        if (gatewayEvent.GuildID.Value.Value != botOptions.Value.ServerId)
        {
            logger.LogError("Ignoring vote for disallowed guild {guildId}", gatewayEvent.GuildID.Value);
            return Result.FromSuccess();
        }

        await voteManager.AddVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        return Result.FromSuccess();
    }
}