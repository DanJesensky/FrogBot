using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders
{
    public class MessageVoteCreationResponder : IResponder<IMessageCreate>
    {
        private static readonly Regex _linkRegex = new("https??://", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private readonly ILogger<MessageVoteCreationResponder> _logger;
        private readonly IDiscordRestChannelAPI _messageApi;

        public MessageVoteCreationResponder(ILogger<MessageVoteCreationResponder> logger, IDiscordRestChannelAPI messageApi)
        {
            _logger = logger;
            _messageApi = messageApi;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Author.IsBot.HasValue && gatewayEvent.Author.IsBot.Value)
            {
                return Result.FromSuccess();
            }

            if (!shouldAddReactions(gatewayEvent))
            {
                return Result.FromSuccess();
            }

            // Not invoking these simultaneously because they may be out of order, which can be confusing
            await _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, VoteEmojiProvider.UpvoteEmoji, ct);
            await _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, VoteEmojiProvider.DownvoteEmoji, ct);

            return Result.FromSuccess();
        }

        private static bool shouldAddReactions(IMessage message)
        {
            var content = message.Content;
            return message.Attachments.Any()
                || _linkRegex.IsMatch(content)
                || (content.Contains("!v ")
                    && !content.Equals("!version"));
        }
    }
}