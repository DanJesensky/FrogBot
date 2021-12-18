using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting;

public interface IUsernameCachingService
{
    public Task<string> GetCachedUsernameAsync(ulong userId, CancellationToken ct = default);

    public Task UpdateCachedUsernameAsync(IUser user, CancellationToken ct = default);

    public Task UpdateCachedUsernameAsync(ulong userId, string username, CancellationToken ct = default);
}