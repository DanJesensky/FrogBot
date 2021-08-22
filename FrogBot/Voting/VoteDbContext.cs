using Microsoft.EntityFrameworkCore;

#nullable disable
namespace FrogBot.Voting
{
    public class VoteDbContext : DbContext
    {
        public VoteDbContext(DbContextOptions<VoteDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Vote>().HasKey(vote => new { vote.ChannelId, vote.MessageId, vote.ReceiverId, vote.VoterId, vote.VoteType });
        }

        public DbSet<Vote> Votes { get; set; }
    }
}