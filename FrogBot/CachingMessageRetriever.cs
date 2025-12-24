using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace FrogBot;

public class CachingMessageRetriever(
    IDiscordRestChannelAPI channelApi,
    ILogger<CachingMessageRetriever> logger,
    BotMemoryCache cache)
    : IMessageRetriever
{
    private const long MessageCacheItemSize = 50L;
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(15);

    public async Task<IMessage?> RetrieveMessageAsync(Snowflake channelId, Snowflake messageId, CancellationToken cancellation = default)
    {
        return await cache.GetOrCreateAsync($"message:{channelId}:{messageId}", async entry =>
        {
            var result = await channelApi.GetChannelMessageAsync(channelId, messageId, cancellation);
            if (!result.IsSuccess)
            {
                logger.LogError("Failed to retrieve message {message} in channel {channel}", messageId, channelId);
                return null;
            }

            entry.SetSize(MessageCacheItemSize + result.Entity.Content.Length)
                .SetSlidingExpiration(SlidingExpiration)
                .SetAbsoluteExpiration(AbsoluteExpiration);

            return result.Entity;
        });
    }
}