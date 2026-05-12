using System.Linq;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace FrogBot.Voting;

public class VoteDbContext(DbContextOptions<VoteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Vote>().HasKey(vote => new { vote.ChannelId, vote.MessageId, vote.ReceiverId, vote.VoterId, vote.VoteType });
    }

    public IQueryable<Vote> AdjustedVotes => Votes
        .Where(v => !BannedVoters.Select(b => b.UserId).Contains(v.VoterId));

    public DbSet<Vote> Votes { get; set; }

    public DbSet<CachedUsername> CachedUsernames { get; set; }

    public DbSet<BannedVoter> BannedVoters { get; set; }
}
