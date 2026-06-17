using FrogBot.Voting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FrogBot.UnitTests;

public class TestHelpers
{
    internal static VoteDbContext CreateVoteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new VoteDbContext(
            new DbContextOptionsBuilder<VoteDbContext>()
                .UseSqlite(connection)
                .Options);
        context.Database.EnsureCreated();
        return context;
    }
}
