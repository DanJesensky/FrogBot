using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrogBot.Voting;

public class VoteManager(VoteDbContext dbContext, ILogger<VoteManager> logger) : IVoteManager
{
    public async Task AddVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type)
    {
        if (await dbContext.BannedVoters.AnyAsync(bannedUser => bannedUser.UserId == voter))
        {
            return;
        }

        try
        {
            _ = await dbContext.Votes.AddAsync(new Vote
            {
                ChannelId = channel,
                MessageId = message,
                ReceiverId = author,
                VoterId = voter,
                VoteType = type
            });
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
            _ = dbContext.Votes.Remove(new Vote
            {
                ChannelId = channel,
                MessageId = message,
                ReceiverId = author,
                VoterId = voter,
                VoteType = type
            });
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not add vote for message {message} in channel {channel}", message, channel);
        }
    }

    public async Task RemoveAllVotesAsync(ulong channel, ulong message)
    {
        var votes = dbContext.Votes.Where(vote => vote.ChannelId == channel && vote.MessageId == message);

        if (!votes.Any())
        {
            logger.LogTrace("No votes to delete for message {message} in channel {channel}.", message, channel);
            return;
        }

        try
        {
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
        await dbContext.Votes
            .AsNoTracking()
            .Where(v =>
                v.ReceiverId == userId 
                && !dbContext.BannedVoters.AsNoTracking().Any(user => user.UserId == v.VoterId))
            .SumAsync(v => (int)v.VoteType);

    public async Task RemoveVotesAsync(ulong channel, ulong message, VoteType type)
    {
        var votesToRemove = dbContext.Votes
            .AsNoTracking()
            .Where(vote => vote.ChannelId == channel && vote.MessageId == message && vote.VoteType == type);

        dbContext.Votes.RemoveRange(votesToRemove);
        await dbContext.SaveChangesAsync();
    }
}