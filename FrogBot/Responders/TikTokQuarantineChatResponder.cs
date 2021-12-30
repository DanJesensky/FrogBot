using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.Responders
{
    public class TikTokChatResponder : IChatResponder, ITikTokQuarantine
    {
        private static readonly Regex _tiktokRegex = new("https??://(?:.*\\.)?tiktok.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly ILogger<TikTokChatResponder> _logger;
        private readonly IDiscordRestChannelAPI _channel;
        private readonly IOptionsMonitor<TikTokOptions> _tiktokOptions;

        public TikTokChatResponder(ILogger<TikTokChatResponder> logger, IDiscordRestChannelAPI channel, IOptionsMonitor<TikTokOptions> tiktokOptions)
        {
            _logger = logger;
            _channel = channel;
            _tiktokOptions = tiktokOptions;
        }

        public async Task<Result> RespondAsync(IMessage message, CancellationToken cancellation = default)
        {
            if (message.ChannelID.Value == _tiktokOptions.CurrentValue.TikTokChannelId)
            {
                _logger.LogDebug("Message {messageId} is in the TikTok channel, ignoring", message.ID);
                return Result.FromSuccess();
            }

            if (!_tiktokRegex.IsMatch(message.Content))
            {
                _logger.LogTrace("Message {messageId} does not contain a TikTok link", message.ID);
                return Result.FromSuccess();
            }

            var deleteTask = _channel.DeleteMessageAsync(message.ChannelID, message.ID, "TikTok links should be posted in the TikTok quarantine channel", cancellation);
            var repostTask = _channel.CreateMessageAsync(new Snowflake(_tiktokOptions.CurrentValue.TikTokChannelId), $"Posted by <@{message.Author.ID}>:\n{message.Content}", ct: cancellation);

            await Task.WhenAll(deleteTask, repostTask);
            return Result.FromSuccess();
        }
    }
}
