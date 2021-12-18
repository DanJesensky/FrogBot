using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FrogBot.ChatCommands;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace FrogBot.UnitTests;

public class VoteBanCommandTests
{
    [Test]
    public async Task HandleCommandAsync_AddsBanToDbContext()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var issuingUser = new User(new Snowflake(1337L), "admin", 1337, null);
        var targetUser = new UserMention(new Snowflake(9999L), "admin", 1234, null);

        var messageCreateEventMock = new Mock<IMessageCreate>();
        messageCreateEventMock.Setup(m => m.ChannelID).Returns(new Snowflake(1000L));
        messageCreateEventMock.Setup(m => m.ID).Returns(new Snowflake(1L));
        messageCreateEventMock.Setup(m => m.Author).Returns(issuingUser);
        messageCreateEventMock.Setup(m => m.Content).Returns("!voteban <@9999>");
        messageCreateEventMock.Setup(m => m.Mentions).Returns(new List<IUserMention> { targetUser });

        var sut = new VoteBanCommand(dbContext, Mock.Of<IDiscordRestChannelAPI>(), Mock.Of<ILogger<VoteBanCommand>>());
        await sut.HandleCommandAsync(messageCreateEventMock.Object);

        dbContext.BannedVoters.Single().UserId.Should().Be(9999L);
    }

    [Test]
    public async Task HandleCommandAsync_MultipleMentions_LogsError()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var issuingUser = new User(new Snowflake(1337L), "admin", 1337, null);
        var targetUser1 = new UserMention(new Snowflake(9999L), "admin", 1234, null);
        var targetUser2 = new UserMention(new Snowflake(9998L), "admin", 4321, null);

        var messageCreateEventMock = new Mock<IMessageCreate>();
        messageCreateEventMock.Setup(m => m.ChannelID).Returns(new Snowflake(1000L));
        messageCreateEventMock.Setup(m => m.ID).Returns(new Snowflake(1L));
        messageCreateEventMock.Setup(m => m.Author).Returns(issuingUser);
        messageCreateEventMock.Setup(m => m.Content).Returns("!voteban <@9999> <@9998>");
        messageCreateEventMock.Setup(m => m.Mentions).Returns(new List<IUserMention> { targetUser1, targetUser2 });

        var logger = new MockLogger<VoteBanCommand>();
        var mockChannelApi = new Mock<IDiscordRestChannelAPI>();
        mockChannelApi.Setup(m => m.CreateReactionAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        var sut = new VoteBanCommand(dbContext, mockChannelApi.Object, logger);
        await sut.HandleCommandAsync(messageCreateEventMock.Object);

        dbContext.BannedVoters.Count().Should().Be(0);
        logger[LogLevel.Error].Should().Contain(message => message.Message.Contains("multiple users"));
        mockChannelApi.Verify(m => m.CreateReactionAsync(new Snowflake(1000L, 0UL), new Snowflake(1L, 0UL), "❌", It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task HandleCommandAsync_UserAlreadyBanned_LogsError()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 9999L });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var issuingUser = new User(new Snowflake(1337L), "admin", 1337, null);
        var targetUser1 = new UserMention(new Snowflake(9999L), "admin", 1234, null);

        var messageCreateEventMock = new Mock<IMessageCreate>();
        messageCreateEventMock.Setup(m => m.ChannelID).Returns(new Snowflake(1000L));
        messageCreateEventMock.Setup(m => m.ID).Returns(new Snowflake(1L));
        messageCreateEventMock.Setup(m => m.Author).Returns(issuingUser);
        messageCreateEventMock.Setup(m => m.Content).Returns("!voteban <@9999>");
        messageCreateEventMock.Setup(m => m.Mentions).Returns(new List<IUserMention> { targetUser1 });

        var logger = new MockLogger<VoteBanCommand>();
        var mockChannelApi = new Mock<IDiscordRestChannelAPI>();
        mockChannelApi.Setup(m => m.CreateReactionAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        var sut = new VoteBanCommand(dbContext, mockChannelApi.Object, logger);
        await sut.HandleCommandAsync(messageCreateEventMock.Object);

        dbContext.BannedVoters.Count().Should().Be(1);
        logger[LogLevel.Error].Should().Contain(message => message.Message.Contains("already banned"));
        mockChannelApi.Verify(m => m.CreateReactionAsync(new Snowflake(1000L, 0UL), new Snowflake(1L, 0UL), "❌", It.IsAny<CancellationToken>()));
    }
}
