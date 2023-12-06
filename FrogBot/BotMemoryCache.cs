using FrogBot.Voting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FrogBot;

public class BotMemoryCache : MemoryCache
{
    public BotMemoryCache(IOptions<BotMemoryCacheOptions> optionsAccessor) : base(optionsAccessor)
    {
    }

    public BotMemoryCache(IOptions<BotMemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory) : base(optionsAccessor, loggerFactory)
    {
    }
}