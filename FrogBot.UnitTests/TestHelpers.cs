using System;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;

namespace FrogBot.UnitTests;

public class TestHelpers
{
    internal static VoteDbContext CreateVoteDbContext(Guid? id = null) =>
        new (new DbContextOptionsBuilder<VoteDbContext>()
            .UseInMemoryDatabase($"vote_{(id ?? Guid.NewGuid()).ToString()}")
            .Options);
}