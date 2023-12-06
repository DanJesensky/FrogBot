using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace FrogBot;

public class CachingMessageRetriever : IMessageRetriever
{
    private const long MessageCacheItemSize = 50L;
    private static readonly TimeSpan _slidingExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _absoluteExpiration = TimeSpan.FromMinutes(15);
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly ILogger<CachingMessageRetriever> _logger;
    private readonly BotMemoryCache _cache;

    public CachingMessageRetriever(IDiscordRestChannelAPI channelApi, ILogger<CachingMessageRetriever> logger, BotMemoryCache cache)
    {
        _channelApi = channelApi;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IMessage?> RetrieveMessageAsync(Snowflake channelId, Snowflake messageId, CancellationToken cancellation = default)
    {
        return await _cache.GetOrCreateAsync($"message:{channelId}:{messageId}", async entry =>
        {
            var result = await _channelApi.GetChannelMessageAsync(channelId, messageId, cancellation);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to retrieve message {message} in channel {channel}", messageId, channelId);
                return null;
            }

            entry.SetSize(MessageCacheItemSize + result.Entity.Content.Length)
                .SetSlidingExpiration(_slidingExpiration)
                .SetAbsoluteExpiration(_absoluteExpiration);

            return result.Entity;
        });
    }
}