using System.Threading;
using System.Threading.Tasks;
using FrogBot.TikTok;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.Responders;

public class TikTokChatResponder(
    IDiscordRestChannelAPI channel,
    IOptionsMonitor<TikTokOptions> tiktokOptions,
    ITikTokQuarantineManager quarantineManager)
    : ITikTokQuarantineResponder
{
    public async Task<Result> RespondAsync(IMessage message, CancellationToken cancellation = default)
    {
        if (!quarantineManager.ShouldMessageBeQuarantined(message))
        {
            return Result.FromSuccess();
        }

        var deleteTask = channel.DeleteMessageAsync(message.ChannelID, message.ID, "TikTok links should be posted in the TikTok quarantine channel", cancellation);
        var repostTask = channel.CreateMessageAsync(new Snowflake(tiktokOptions.CurrentValue.TikTokChannelId), $"Posted by <@{message.Author.ID}>:\n{message.Content}", ct: cancellation);

        await Task.WhenAll(deleteTask, repostTask);
        return Result.FromSuccess();
    }
}