using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using FrogBot.Extensions;

namespace FrogBot.Voting
{
    public class UsernameCachingService : IUsernameCachingService
    {
        private readonly VoteDbContext _dbContext;
        private readonly IDiscordRestUserAPI _userApi;

        public UsernameCachingService(VoteDbContext dbContext, IDiscordRestUserAPI userApi)
        {
            _dbContext = dbContext;
            _userApi = userApi;
        }

        public async Task<string> GetCachedUsernameAsync(ulong userId, CancellationToken ct = default)
        {
            var usernameCache = await _dbContext.CachedUsernames
                .FirstOrDefaultAsync(user => user.UserId == userId, ct);
            if (usernameCache != null)
            {
                return usernameCache.Username;
            }

            var fetchUserResult = await _userApi.GetUserAsync(new Snowflake(userId), ct);
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
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            var cachedUser = await _dbContext.CachedUsernames
                .FirstOrDefaultAsync(cachedUser => cachedUser.UserId == userId, ct);

            if (cachedUser == null)
            {
                await _dbContext.CachedUsernames.AddAsync(new CachedUsername
                {
                    UserId = userId,
                    Username = username
                }, ct);
            }
            else
            {
                cachedUser.Username = username;
            }

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
    }
}