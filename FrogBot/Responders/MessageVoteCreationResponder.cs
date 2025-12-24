using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using FrogBot.Voting;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

[UsedImplicitly]
public partial class MessageVoteCreationResponder(
    ILogger<MessageVoteCreationResponder> logger,
    IOptions<VoteOptions> voteOptions,
    IDiscordRestChannelAPI messageApi,
    ITikTokQuarantineManager tikTokQuarantine)
    : IResponder<IMessageCreate>
{
    private static readonly Regex LinkRegex = GenerateLinkRegex();

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        var author = tikTokQuarantine.GetSubstituteQuarantineAuthor(gatewayEvent);
        if (author.IsBot.HasValue && author.IsBot.Value)
        {
            return Result.FromSuccess();
        }

        if (!ShouldAddReactions(gatewayEvent))
        {
            return Result.FromSuccess();
        }

        // Not invoking these simultaneously because they may be out of order, which can be confusing
        await messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, voteOptions.Value.BotUpvoteEmoji, ct);
        await messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, voteOptions.Value.BotDownvoteEmoji, ct);

        logger.LogDebug("Added bot reactions to message {messageId}", gatewayEvent.ID);
        return Result.FromSuccess();
    }

    private static bool ShouldAddReactions(IMessage message)
    {
        var content = message.Content;
        return message.Attachments.Any()
               || LinkRegex.IsMatch(content)
               || (content.Contains("!v")
                   && !content.Equals("!version"));
    }

    [GeneratedRegex("https??://", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
    private static partial Regex GenerateLinkRegex();
}