using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.Responders
{
    public class TikTokChatResponder : IChatResponder
    {
        private static Regex _tiktokRegex = new("https??://(?:.*\\.)?tiktok.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly ILogger<TikTokChatResponder> _logger;
        private readonly IDiscordRestChannelAPI _channel;
        private readonly IOptionsMonitor<TikTokOptions> _tiktokOptions;

        public TikTokChatResponder(ILogger<TikTokChatResponder> logger, IDiscordRestChannelAPI channel, IOptionsMonitor<TikTokOptions> tiktokOptions)
        {
            _logger = logger;
            _channel = channel;
            _tiktokOptions = tiktokOptions;
        }

        public async Task<Result> RespondAsync(IMessage message, IMessageCreate gatewayEvent, CancellationToken cancellation = default)
        {
            if (gatewayEvent.ChannelID.Value == _tiktokOptions.CurrentValue.TikTokChannelId)
            {
                _logger.LogDebug("Message {messageId} is in the TikTok channel, ignoring", gatewayEvent.ID);
                return Result.FromSuccess();
            }

            if (!_tiktokRegex.IsMatch(message.Content))
            {
                _logger.LogTrace("Message {messageId} does not contain a TikTok link", gatewayEvent.ID);
                return Result.FromSuccess();
            }

            var deleteTask = _channel.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, "TikTok links should be posted in the TikTok quarantine channel", cancellation);
            var repostTask = _channel.CreateMessageAsync(new Snowflake(_tiktokOptions.CurrentValue.TikTokChannelId), $"Posted by <@{message.Author.ID}>:\n{message.Content}", ct: cancellation);

            await Task.WhenAll(deleteTask, repostTask);
            return Result.FromSuccess();
        }
    }
}
