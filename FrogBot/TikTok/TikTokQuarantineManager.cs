using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.TikTok;

public partial class TikTokQuarantineManager(
    IOptionsMonitor<TikTokOptions> tiktokOptions,
    IOptionsMonitor<FrogBotOptions> botOptions,
    ILogger<TikTokQuarantineManager> logger)
    : ITikTokQuarantineManager
{
    private static readonly Regex TiktokRegex = GenerateTiktokRegex();

    public bool ShouldMessageBeQuarantined(IMessage message)
    {
        if (message.ChannelID.Value == tiktokOptions.CurrentValue.TikTokChannelId)
        {
            logger.LogDebug("Message {messageId} is in the TikTok channel, ignoring", message.ID);
            return false;
        }

        if (!TiktokRegex.IsMatch(message.Content))
        {
            logger.LogTrace("Message {messageId} does not contain a TikTok link", message.ID);
            return false;
        }

        return true;
    }
    
    public IUser GetSubstituteQuarantineAuthor(IMessage message)
    {
        // Users can't vote on bots, except for in the TikTok quarantine.
        // In that case, the vote is attributed to the original author whose message was deleted for quarantine.
        var author = message.Author;
        if (author.ID.Value != botOptions.CurrentValue.BotUserId || 
            message.ChannelID.Value != tiktokOptions.CurrentValue.TikTokChannelId)
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

    [GeneratedRegex("https??://(?:.*\\.)?tiktok.com", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex GenerateTiktokRegex();
}