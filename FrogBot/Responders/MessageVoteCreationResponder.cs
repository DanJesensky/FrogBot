using System.Linq;
using System.Text.RegularExpressions;
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
using Remora.Results;

namespace FrogBot.Responders;

public class MessageVoteCreationResponder : IResponder<IMessageCreate>
{
    private static readonly Regex _linkRegex = new("https??://", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private readonly ILogger<MessageVoteCreationResponder> _logger;
    private readonly IOptions<VoteOptions> _voteOptions;
    private readonly IDiscordRestChannelAPI _messageApi;
    private readonly ITikTokQuarantineManager _tikTokQuarantine;

    public MessageVoteCreationResponder(ILogger<MessageVoteCreationResponder> logger, IOptions<VoteOptions> voteOptions, IDiscordRestChannelAPI messageApi, ITikTokQuarantineManager tikTokQuarantine)
    {
        _logger = logger;
        _voteOptions = voteOptions;
        _messageApi = messageApi;
        _tikTokQuarantine = tikTokQuarantine;
    }

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        var author = _tikTokQuarantine.GetSubstituteQuarantineAuthor(gatewayEvent);
        if (author.IsBot.HasValue && author.IsBot.Value)
        {
            return Result.FromSuccess();
        }

        if (!shouldAddReactions(gatewayEvent))
        {
            return Result.FromSuccess();
        }

        // Not invoking these simultaneously because they may be out of order, which can be confusing
        await _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, _voteOptions.Value.BotUpvoteEmoji, ct);
        await _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, _voteOptions.Value.BotDownvoteEmoji, ct);

        _logger.LogDebug("Added bot reactions to message {messageId}", gatewayEvent.ID);
        return Result.FromSuccess();
    }

    private static bool shouldAddReactions(IMessage message)
    {
        var content = message.Content;
        return message.Attachments.Any()
               || _linkRegex.IsMatch(content)
               || (content.Contains("!v")
                   && !content.Equals("!version"));
    }
}