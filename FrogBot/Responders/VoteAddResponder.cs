using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.Responders;

public class VoteAddResponder(
    ILogger<VoteAddResponder> logger,
    IOptions<FrogBotOptions> botOptions,
    IVoteManager voteManager,
    IMessageRetriever messageRetriever,
    IVoteEmojiProvider voteEmojiProvider,
    ITikTokQuarantineManager quarantine,
    IDiscordRestChannelAPI messageApi)
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

        // Banned voters don't count and shouldn't trigger bot reactions.
        if (await voteManager.IsVoterBannedAsync(gatewayEvent.UserID.Value))
        {
            logger.LogDebug("Ignoring vote from banned voter {userId} on message {messageId}", gatewayEvent.UserID, gatewayEvent.MessageID);
            return Result.FromSuccess();
        }

        await voteManager.AddVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, author.ID.Value,
            gatewayEvent.UserID.Value, voteType.Value);

        // Ensure both vote reactions exist on the message so users can vote in either direction.
        // Only add reactions that the bot hasn't already placed to avoid redundant REST calls.
        var upvoteEmoji = voteEmojiProvider.GetEmoji(VoteType.Upvote);
        var downvoteEmoji = voteEmojiProvider.GetEmoji(VoteType.Downvote);
        if (upvoteEmoji is not null && !HasBotReaction(message, upvoteEmoji))
        {
            var upvoteResult = await messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, upvoteEmoji, ct);
            if (!upvoteResult.IsSuccess)
            {
                logger.LogWarning("Failed to add upvote reaction to message {messageId}: {error}", gatewayEvent.MessageID, upvoteResult.Error);
            }
        }
        if (downvoteEmoji is not null && !HasBotReaction(message, downvoteEmoji))
        {
            var downvoteResult = await messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, downvoteEmoji, ct);
            if (!downvoteResult.IsSuccess)
            {
                logger.LogWarning("Failed to add downvote reaction to message {messageId}: {error}", gatewayEvent.MessageID, downvoteResult.Error);
            }
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Returns true if the bot (current user) has already placed the given emoji reaction on the message.
    /// </summary>
    private static bool HasBotReaction(IMessage message, string emojiString)
    {
        if (!message.Reactions.HasValue)
            return false;

        return message.Reactions.Value.Any(r =>
            r.HasCurrentUserReacted && MatchesEmoji(r.Emoji, emojiString));
    }

    private static bool MatchesEmoji(IPartialEmoji emoji, string emojiString)
    {
        // Custom emoji: string format is "name:id" — compare by numeric ID.
        if (emoji.ID.HasValue && emoji.ID.Value is Snowflake snowflake)
        {
            var idStr = snowflake.Value.ToString();
            return emojiString.EndsWith($":{idStr}") || emojiString == idStr;
        }

        // Unicode emoji: compare by name.
        return emoji.Name.HasValue && emoji.Name.Value == emojiString;
    }
}