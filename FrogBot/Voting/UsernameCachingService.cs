using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using FrogBot.Extensions;
using Remora.Rest.Core;

namespace FrogBot.Voting;

public class UsernameCachingService(VoteDbContext dbContext, IDiscordRestUserAPI userApi) : IUsernameCachingService
{
    public async Task<string> GetCachedUsernameAsync(ulong userId, CancellationToken ct = default)
    {
        var usernameCache = await dbContext.CachedUsernames
            .FirstOrDefaultAsync(user => user.UserId == userId, ct);
        if (usernameCache != null)
        {
            return usernameCache.Username;
        }

        var fetchUserResult = await userApi.GetUserAsync(new Snowflake(userId), ct);
        if (!fetchUserResult.IsSuccess)
        {
            throw new ArgumentException("User ID is invalid.", nameof(userId));
        }

        var user = fetchUserResult.Entity;
        await UpdateCachedUsernameAsync(user, ct);
        return user.GetFullUsername();
    }

    public async Task UpdateCachedUsernameAsync(IUser user, CancellationToken ct = default) =>
        await UpdateCachedUsernameAsync(user.ID.Value, user.GetFullUsername(), ct);

    public async Task UpdateCachedUsernameAsync(ulong userId, string username, CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var cachedUser = await dbContext.CachedUsernames
            .FirstOrDefaultAsync(cachedUser => cachedUser.UserId == userId, ct);

        if (cachedUser == null)
        {
            await dbContext.CachedUsernames.AddAsync(new CachedUsername
            {
                UserId = userId,
                Username = username
            }, ct);
        }
        else
        {
            cachedUser.Username = username;
        }

        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}