using System.Threading;
using System.Threading.Tasks;
using FrogBot.Extensions;
using FrogBot.Voting;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders;

public class UsernameChangeResponder : IResponder<IUserUpdate>
{
    private readonly IUsernameCachingService _usernameCache;

    public UsernameChangeResponder(IUsernameCachingService usernameCache)
    {
        _usernameCache = usernameCache;
    }

    public async Task<Result> RespondAsync(IUserUpdate gatewayEvent, CancellationToken ct = new())
    {
        await _usernameCache.UpdateCachedUsernameAsync(gatewayEvent.ID.Value, gatewayEvent.GetFullUsername(), ct);
        return Result.FromSuccess();
    }
}