using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.TikTok;

public class TikTokQuarantineManager : ITikTokQuarantineManager
{
    private static readonly Regex _tiktokRegex = new("https??://(?:.*\\.)?tiktok.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly IOptionsMonitor<TikTokOptions> _tiktokOptions;
    private readonly IOptionsMonitor<FrogBotOptions> _botOptions;
    private readonly ILogger<TikTokQuarantineManager> _logger;

    public TikTokQuarantineManager(IOptionsMonitor<TikTokOptions> tiktokOptions, IOptionsMonitor<FrogBotOptions> botOptions, ILogger<TikTokQuarantineManager> logger)
    {
        _tiktokOptions = tiktokOptions;
        _botOptions = botOptions;
        _logger = logger;
    }

    public bool ShouldMessageBeQuarantined(IMessage message)
    {
        if (message.ChannelID.Value == _tiktokOptions.CurrentValue.TikTokChannelId)
        {
            _logger.LogDebug("Message {messageId} is in the TikTok channel, ignoring", message.ID);
            return false;
        }

        if (!_tiktokRegex.IsMatch(message.Content))
        {
            _logger.LogTrace("Message {messageId} does not contain a TikTok link", message.ID);
            return false;
        }

        return true;
    }
    
    public IUser GetSubstituteQuarantineAuthor(IMessage message)
    {
        // Users can't vote on bots, except for in the TikTok quarantine.
        // In that case, the vote is attributed to the original author whose message was deleted for quarantine.
        var author = message.Author;
        if (author.ID.Value != _botOptions.CurrentValue.BotUserId || 
            message.ChannelID.Value != _tiktokOptions.CurrentValue.TikTokChannelId)
        {
            return author;
        }

        // If there are multiple mentions or the format isn't right, it's probably a !points or !top command
        // response and should be attributed to the bot.
        author = message.Mentions.SingleOrDefault();
        if (author == null || !message.Content.StartsWith($"Posted by <@{author.ID.Value}>:"))
        {
            return message.Author;
        }

        return author;
    }
}