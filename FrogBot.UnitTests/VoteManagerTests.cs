using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FrogBot.UnitTests;

public class VoteManagerTests
{
    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task AddVoteAsync_RecordsVote(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 2, 3, 4, voteType);
        dbContext.Votes.Should().Contain(vote =>
            vote.ChannelId == 1
            && vote.MessageId == 2
            && vote.ReceiverId == 3
            && vote.VoterId == 4
            && vote.VoteType == voteType);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task AddVoteAsync_VoterBanned_StillRecordsVote(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 4 });
        
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 2, 3, 4, voteType);
        dbContext.Votes.Should().Contain(vote =>
            vote.ChannelId == 1
            && vote.MessageId == 2
            && vote.ReceiverId == 3
            && vote.VoterId == 4
            && vote.VoteType == voteType);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task RemoveVoteAsync_RemovesVote(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        
        await sut.AddVoteAsync(1, 2, 3, 4, voteType);

        dbContext.Votes.Should().Contain(vote =>
            vote.ChannelId == 1
            && vote.MessageId == 2
            && vote.ReceiverId == 3
            && vote.VoterId == 4
            && vote.VoteType == voteType);

        dbContext.ChangeTracker.Clear();
        await sut.RemoveVoteAsync(1, 2, 3, 4, voteType);
        dbContext.Votes.Count().Should().Be(0);
    }

    [Test]
    [TestCase(VoteType.Upvote, ExpectedResult = 1)]
    [TestCase(VoteType.Downvote, ExpectedResult = -1)]
    public async Task<long> GetScoreAsync_IncrementsOrDecrementsScore(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.Votes.Add(new Vote { ChannelId = 1, MessageId = 1, ReceiverId = 1337, VoterId = 1234, VoteType = voteType });
        await dbContext.SaveChangesAsync();
        dbContext.Votes.Count().Should().Be(1);

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        return await sut.GetScoreAsync(1337L);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task AddVoteAsync_VoterBanned_DoesNotRecordVote(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 4 });
        await dbContext.SaveChangesAsync();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 2, 3, 4, voteType);

        dbContext.Votes.Should().BeEmpty();
    }

    [Test]
    public async Task IsVoterBannedAsync_CacheHit_ReturnsCachedResult()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 4 });
        await dbContext.SaveChangesAsync();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        // First call hits the DB and populates the cache.
        var firstResult = await sut.IsVoterBannedAsync(4);
        firstResult.Should().BeTrue();

        // Remove the record from the DB so that a live DB query would return false.
        dbContext.BannedVoters.RemoveRange(dbContext.BannedVoters);
        await dbContext.SaveChangesAsync();

        // Second call must return the cached value (true), not the live DB value (false).
        var secondResult = await sut.IsVoterBannedAsync(4);
        secondResult.Should().BeTrue();
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task GetScoreAsync_IgnoresBannedUserVotes(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 9999 });
        dbContext.Votes.AddRange(
            new Vote { ChannelId = 1, MessageId = 1, ReceiverId = 1337, VoterId = 1234, VoteType = VoteType.Upvote },
            new Vote { ChannelId = 1, MessageId = 1, ReceiverId = 1337, VoterId = 9999, VoteType = voteType });
        await dbContext.SaveChangesAsync();
        dbContext.Votes.Count().Should().Be(2);

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        var score = await sut.GetScoreAsync(1337L);
        score.Should().Be(1);
    }
}