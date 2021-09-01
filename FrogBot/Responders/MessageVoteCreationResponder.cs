using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

            var tasks = new[]
            {
                _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, "⬆", ct),
                _messageApi.CreateReactionAsync(gatewayEvent.ChannelID, gatewayEvent.ID, "⬇", ct)
            };

            await Task.WhenAll(tasks);
            return Result.FromSuccess();
        }

        private static bool shouldAddReactions(IMessage message)
        {
            var content = message.Content;
            return message.Attachments.Any()
                || _linkRegex.IsMatch(content)
                || content.Contains("!v");
        }
    }
}