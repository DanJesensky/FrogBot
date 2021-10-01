using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrogBot.Voting
{
    public class VoteManager : IVoteManager
    {
        private readonly VoteDbContext _dbContext;
        private readonly ILogger<VoteManager> _logger;

        public VoteManager(VoteDbContext dbContext, ILogger<VoteManager> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task AddVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type)
        {
            try
            {
                _ = await _dbContext.Votes.AddAsync(new Vote
                {
                    ChannelId = channel,
                    MessageId = message,
                    ReceiverId = author,
                    VoterId = voter,
                    VoteType = type
                });
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not add vote for message {message} in channel {channel}", message, channel);
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

            if (!await _dbContext.Votes.ContainsAsync(vote))
            {
                return;
            }

            try
            {
                _ = _dbContext.Votes.Remove(new Vote
                {
                    ChannelId = channel,
                    MessageId = message,
                    ReceiverId = author,
                    VoterId = voter,
                    VoteType = type
                });
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not add vote for message {message} in channel {channel}", message, channel);
            }
        }

        public async Task RemoveAllVotesAsync(ulong channel, ulong message)
        {
            var votes = _dbContext.Votes.Where(vote => vote.ChannelId == channel && vote.MessageId == message);

            if (!votes.Any())
            {
                _logger.LogTrace("No votes to delete for message {message} in channel {channel}.", message, channel);
                return;
            }

            try
            {
                _dbContext.Votes.RemoveRange(votes);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete votes for message {message} in {channel}", message, channel);
            }
        }

        public async Task<IEnumerable<Vote>> GetVotesAsync(ulong channel, ulong message) =>
            await _dbContext.Votes
                .AsNoTracking()
                .Where(vote => vote.ChannelId == channel && vote.MessageId == message)
                .ToArrayAsync();

        public async Task RemoveVotesAsync(ulong channel, ulong message, VoteType type)
        {
            var votesToRemove = _dbContext.Votes
                .AsNoTracking()
                .Where(vote => vote.ChannelId == channel && vote.MessageId == message && vote.VoteType == type);
            
            _dbContext.Votes.RemoveRange(votesToRemove);
            await _dbContext.SaveChangesAsync();
        }
    }
}