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
    public async Task AddVoteAsync_CreatesBalanceForNewReceiver(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, voteType);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance.Should().NotBeNull();
        balance!.Balance.Should().Be((long)voteType);
    }

    [Test]
    public async Task AddVoteAsync_AccumulatesBalanceForExistingReceiver()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(1, 2, author: 100, voter: 202, VoteType.Upvote);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance!.Balance.Should().Be(2);
    }

    [Test]
    [TestCase(VoteType.Upvote, PointLedgerReason.Upvote)]
    [TestCase(VoteType.Downvote, PointLedgerReason.Downvote)]
    public async Task AddVoteAsync_WritesLedgerEntry(VoteType voteType, PointLedgerReason expectedReason)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, voteType);

        dbContext.PointLedgerEntries.Should().ContainSingle(e =>
            e.UserId == 100
            && e.Delta == (long)voteType
            && e.Reason == expectedReason);
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
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task RemoveVoteAsync_AdjustsBalance(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, voteType);
        dbContext.ChangeTracker.Clear();
        await sut.RemoveVoteAsync(1, 1, author: 100, voter: 200, voteType);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance!.Balance.Should().Be(0);
    }

    [Test]
    [TestCase(VoteType.Upvote, PointLedgerReason.UpvoteRemoved)]
    [TestCase(VoteType.Downvote, PointLedgerReason.DownvoteRemoved)]
    public async Task RemoveVoteAsync_WritesLedgerEntry(VoteType voteType, PointLedgerReason expectedReason)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, voteType);
        dbContext.ChangeTracker.Clear();
        await sut.RemoveVoteAsync(1, 1, author: 100, voter: 200, voteType);

        dbContext.PointLedgerEntries.Should().Contain(e =>
            e.UserId == 100
            && e.Delta == -(long)voteType
            && e.Reason == expectedReason);
    }

    [Test]
    [TestCase(VoteType.Upvote, ExpectedResult = 1)]
    [TestCase(VoteType.Downvote, ExpectedResult = -1)]
    public async Task<long> GetScoreAsync_ReturnsBalance(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 1, author: 1337, voter: 1234, voteType);
        return await sut.GetScoreAsync(1337);
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
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task AddVoteAsync_VoterBanned_DoesNotUpdateBalance(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 4 });
        await dbContext.SaveChangesAsync();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());
        await sut.AddVoteAsync(1, 2, author: 3, voter: 4, voteType);

        dbContext.UserPointBalances.Should().BeEmpty();
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task GetScoreAsync_BannedVoterCannotContributeToBalance(VoteType voteType)
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 9999 });
        await dbContext.SaveChangesAsync();

        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        // Non-banned voter upvotes: contributes +1
        await sut.AddVoteAsync(1, 1, author: 1337, voter: 1234, VoteType.Upvote);

        // Banned voter attempts to vote: blocked, contributes nothing
        await sut.AddVoteAsync(1, 2, author: 1337, voter: 9999, voteType);

        var score = await sut.GetScoreAsync(1337);
        score.Should().Be(1);
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
    public async Task TrySpendPointsAsync_SufficientBalance_ReturnsTrueAndDecrementsBalance()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, VoteType.Upvote);
        await sut.AddVoteAsync(1, 2, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(1, 3, author: 100, voter: 202, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        var result = await sut.TrySpendPointsAsync(100, cost: 2, note: "test purchase");

        result.Should().BeTrue();
        var balance = await sut.GetScoreAsync(100);
        balance.Should().Be(1);
    }

    [Test]
    public async Task TrySpendPointsAsync_SufficientBalance_WritesLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, VoteType.Upvote);
        await sut.AddVoteAsync(1, 2, author: 100, voter: 201, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        await sut.TrySpendPointsAsync(100, cost: 1, note: "test purchase");

        dbContext.PointLedgerEntries.Should().Contain(e =>
            e.UserId == 100
            && e.Delta == -1
            && e.Reason == PointLedgerReason.Spent
            && e.Note == "test purchase");
    }

    [Test]
    public async Task TrySpendPointsAsync_InsufficientBalance_ReturnsFalseAndLeavesBalanceUnchanged()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        var result = await sut.TrySpendPointsAsync(100, cost: 5);

        result.Should().BeFalse();
        var balance = await sut.GetScoreAsync(100);
        balance.Should().Be(1);
    }

    [Test]
    public async Task TrySpendPointsAsync_InsufficientBalance_DoesNotWriteLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(1, 1, author: 100, voter: 200, VoteType.Upvote);
        var ledgerCountBefore = dbContext.PointLedgerEntries.Count();
        dbContext.ChangeTracker.Clear();

        await sut.TrySpendPointsAsync(100, cost: 5);

        dbContext.PointLedgerEntries.Count().Should().Be(ledgerCountBefore);
    }

    [Test]
    public async Task RemoveAllVotesAsync_RemovesAllVotesForMessage()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        dbContext.Votes.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllVotesAsync_AdjustsReceiverBalance()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance!.Balance.Should().Be(0);
    }

    [Test]
    public async Task RemoveAllVotesAsync_WritesVotesClearedLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        dbContext.PointLedgerEntries.Should().Contain(e =>
            e.UserId == 100
            && e.Delta == -2
            && e.Reason == PointLedgerReason.VotesCleared);
    }

    [Test]
    public async Task RemoveAllVotesAsync_NoVotes_DoesNotWriteLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        dbContext.PointLedgerEntries.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllVotesAsync_LeavesVotesForOtherMessagesIntact()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 2, author: 100, voter: 202, VoteType.Upvote);

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        dbContext.Votes.Should().ContainSingle(v => v.MessageId == 2);
    }

    [Test]
    public async Task RemoveAllVotesAsync_NetZeroDelta_RemovesVotesButDoesNotWriteLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Downvote);
        var ledgerCountBefore = dbContext.PointLedgerEntries.Count();
        dbContext.ChangeTracker.Clear();

        await sut.RemoveAllVotesAsync(channel: 1, message: 1);

        dbContext.Votes.Should().BeEmpty();
        dbContext.PointLedgerEntries.Count().Should().Be(ledgerCountBefore);
    }

    [Test]
    public async Task RemoveVotesAsync_RemovesOnlySpecifiedVoteType()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Downvote);

        await sut.RemoveVotesAsync(channel: 1, message: 1, VoteType.Upvote);

        dbContext.Votes.Should().ContainSingle(v => v.VoteType == VoteType.Downvote);
    }

    [Test]
    public async Task RemoveVotesAsync_AdjustsBalance()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        await sut.RemoveVotesAsync(channel: 1, message: 1, VoteType.Upvote);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance!.Balance.Should().Be(0);
    }

    [Test]
    public async Task RemoveVotesAsync_WritesVotesClearedLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);
        dbContext.ChangeTracker.Clear();

        await sut.RemoveVotesAsync(channel: 1, message: 1, VoteType.Upvote);

        dbContext.PointLedgerEntries.Should().Contain(e =>
            e.UserId == 100
            && e.Delta == -2
            && e.Reason == PointLedgerReason.VotesCleared);
    }

    [Test]
    public async Task RemoveVotesAsync_NoMatchingVotes_DoesNotWriteLedgerEntry()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Downvote);
        var ledgerCountBefore = dbContext.PointLedgerEntries.Count();
        dbContext.ChangeTracker.Clear();

        await sut.RemoveVotesAsync(channel: 1, message: 1, VoteType.Upvote);

        dbContext.PointLedgerEntries.Count().Should().Be(ledgerCountBefore);
    }

    [Test]
    public async Task RemoveVotesAsync_OnlyAdjustsBalanceForRemovedType()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        var sut = new VoteManager(dbContext, Mock.Of<ILogger<VoteManager>>());

        // Three upvotes and one downvote → balance = 2
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 201, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 202, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 203, VoteType.Upvote);
        await sut.AddVoteAsync(channel: 1, message: 1, author: 100, voter: 204, VoteType.Downvote);
        dbContext.ChangeTracker.Clear();

        // Remove only the upvotes; downvote remains, so balance = -1
        await sut.RemoveVotesAsync(channel: 1, message: 1, VoteType.Upvote);

        var balance = dbContext.UserPointBalances.Find((ulong)100);
        balance!.Balance.Should().Be(-1);
    }
}
