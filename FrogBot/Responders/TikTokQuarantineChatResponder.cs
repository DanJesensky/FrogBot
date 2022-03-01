using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.Responders;

public class TikTokChatResponder : IChatResponder, ITikTokQuarantineResponder
{
    private readonly IDiscordRestChannelAPI _channel;
    private readonly IOptionsMonitor<TikTokOptions> _tiktokOptions;
    private readonly ITikTokQuarantineManager _quarantineManager;

    public TikTokChatResponder(IDiscordRestChannelAPI channel, IOptionsMonitor<TikTokOptions> tiktokOptions, ITikTokQuarantineManager quarantineManager)
    {
        _channel = channel;
        _tiktokOptions = tiktokOptions;
        _quarantineManager = quarantineManager;
    }

    public async Task<Result> RespondAsync(IMessage message, CancellationToken cancellation = default)
    {
        if (!_quarantineManager.ShouldMessageBeQuarantined(message))
        {
            return Result.FromSuccess();
        }

        var deleteTask = _channel.DeleteMessageAsync(message.ChannelID, message.ID, "TikTok links should be posted in the TikTok quarantine channel", cancellation);
        var repostTask = _channel.CreateMessageAsync(new Snowflake(_tiktokOptions.CurrentValue.TikTokChannelId), $"Posted by <@{message.Author.ID}>:\n{message.Content}", ct: cancellation);

        await Task.WhenAll(deleteTask, repostTask);
        return Result.FromSuccess();
    }
}