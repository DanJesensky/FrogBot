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
        modelBuilder.Entity<UserPointBalance>().Property(e => e.UserId).HasConversion<long>();
        modelBuilder.Entity<PointLedgerEntry>().Property(e => e.UserId).HasConversion<long>();
    }

    public IQueryable<Vote> AdjustedVotes => Votes
        .Where(v => !BannedVoters.Any(b => b.UserId == v.VoterId));

    public DbSet<Vote> Votes { get; set; }
    public DbSet<CachedUsername> CachedUsernames { get; set; }
    public DbSet<BannedVoter> BannedVoters { get; set; }
    public DbSet<UserPointBalance> UserPointBalances { get; set; }
    public DbSet<PointLedgerEntry> PointLedgerEntries { get; set; }
}
