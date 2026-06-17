using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrogBot.Voting;

public class VoteManager(VoteDbContext dbContext, ILogger<VoteManager> logger) : IVoteManager
{
    // Transient cache: VoteManager is registered as transient, so this lives only for the
    // duration of a single response chain and is discarded afterwards.
    private readonly ConcurrentDictionary<ulong, bool> _bannedVoterCache = new();

    public async Task AddVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type)
    {
        if (await IsVoterBannedAsync(voter))
        {
            return;
        }

        try
        {
            var delta = (long)type;

            await dbContext.Votes.AddAsync(new Vote
            {
                ChannelId = channel,
                MessageId = message,
                ReceiverId = author,
                VoterId = voter,
                VoteType = type
            });

            dbContext.PointLedgerEntries.Add(new PointLedgerEntry
            {
                UserId = author,
                Delta = delta,
                Reason = type == VoteType.Upvote ? PointLedgerReason.Upvote : PointLedgerReason.Downvote,
                Timestamp = DateTimeOffset.UtcNow
            });

            var balance = await dbContext.UserPointBalances.FindAsync(author);
            if (balance is null)
            {
                dbContext.UserPointBalances.Add(new UserPointBalance { UserId = author, Balance = delta });
            }
            else
            {
                balance.Balance += delta;
            }

            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not add vote for message {message} in channel {channel}", message, channel);
        }
    }

    public async Task RemoveVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type)
    {
        var vote = new Vote
        {
            ChannelId = channel,
            MessageId = message,
            ReceiverId = author,
            VoterId = voter,
            VoteType = type
        };

        if (!await dbContext.Votes.ContainsAsync(vote))
        {
            return;
        }

        try
        {
            var delta = -(long)type;

            dbContext.Votes.Remove(vote);

            dbContext.PointLedgerEntries.Add(new PointLedgerEntry
            {
                UserId = author,
                Delta = delta,
                Reason = type == VoteType.Upvote ? PointLedgerReason.UpvoteRemoved : PointLedgerReason.DownvoteRemoved,
                Timestamp = DateTimeOffset.UtcNow
            });

            var balance = await dbContext.UserPointBalances.FindAsync(author);
            if (balance is not null)
            {
                balance.Balance += delta;
            }

            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not remove vote for message {message} in channel {channel}", message, channel);
        }
    }

    public async Task RemoveAllVotesAsync(ulong channel, ulong message)
    {
        var votes = await dbContext.Votes
            .Where(v => v.ChannelId == channel && v.MessageId == message)
            .ToArrayAsync();

        if (votes.Length == 0)
        {
            logger.LogTrace("No votes to delete for message {message} in channel {channel}.", message, channel);
            return;
        }

        try
        {
            await AdjustBalancesForRemovedVotesAsync(votes);
            dbContext.Votes.RemoveRange(votes);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to delete votes for message {message} in {channel}", message, channel);
        }
    }

    public async Task<IEnumerable<Vote>> GetMessageVotesAsync(ulong channel, ulong message) =>
        await dbContext.Votes
            .AsNoTracking()
            .Where(vote =>
                vote.ChannelId == channel
                && vote.MessageId == message
                && !dbContext.BannedVoters.AsNoTracking().Any(u => u.UserId == vote.VoterId))
            .ToArrayAsync();

    public async Task<long> GetScoreAsync(ulong userId) =>
        await dbContext.UserPointBalances
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => b.Balance)
            .FirstOrDefaultAsync();

    public async Task RemoveVotesAsync(ulong channel, ulong message, VoteType type)
    {
        var votesToRemove = await dbContext.Votes
            .Where(v => v.ChannelId == channel && v.MessageId == message && v.VoteType == type)
            .ToArrayAsync();

        if (votesToRemove.Length == 0)
        {
            return;
        }

        await AdjustBalancesForRemovedVotesAsync(votesToRemove);
        dbContext.Votes.RemoveRange(votesToRemove);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsVoterBannedAsync(ulong voter)
    {
        if (_bannedVoterCache.TryGetValue(voter, out var cached))
        {
            return cached;
        }

        var isBanned = await dbContext.BannedVoters.AnyAsync(bannedUser => bannedUser.UserId == voter);
        _bannedVoterCache.TryAdd(voter, isBanned);
        return isBanned;
    }

    public async Task<bool> TrySpendPointsAsync(ulong userId, long cost, string? note = null)
    {
        if (cost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost must be positive.");
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var rows = await dbContext.UserPointBalances
            .Where(b => b.UserId == userId && b.Balance >= cost)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Balance, b => b.Balance - cost));

        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        dbContext.PointLedgerEntries.Add(new PointLedgerEntry
        {
            UserId = userId,
            Delta = -cost,
            Reason = PointLedgerReason.Spent,
            Timestamp = DateTimeOffset.UtcNow,
            Note = note
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    private async Task AdjustBalancesForRemovedVotesAsync(Vote[] votes)
    {
        var adjustments = votes
            .GroupBy(v => v.ReceiverId)
            .Select(g => new { UserId = g.Key, Delta = -(long)g.Sum(v => (int)v.VoteType) });

        foreach (var adj in adjustments)
        {
            var balance = await dbContext.UserPointBalances.FindAsync(adj.UserId);
            if (balance is not null)
            {
                balance.Balance += adj.Delta;
            }

            if (adj.Delta != 0)
            {
                dbContext.PointLedgerEntries.Add(new PointLedgerEntry
                {
                    UserId = adj.UserId,
                    Delta = adj.Delta,
                    Reason = PointLedgerReason.VotesCleared,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
