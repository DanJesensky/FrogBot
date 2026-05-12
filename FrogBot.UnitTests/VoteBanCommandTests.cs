using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FrogBot.SlashCommands;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.UnitTests;

public class VoteBanCommandTests
{
    private static ICommandContext CreateAdminContext()
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.ID).Returns(new Snowflake(159870805390524416UL));

        var member = new Mock<IGuildMember>();
        member.Setup(m => m.User).Returns(new Optional<IUser>(user.Object));

        var interaction = new Mock<IInteraction>();
        interaction.Setup(i => i.Member).Returns(new Optional<IGuildMember>(member.Object));

        var ctx = new Mock<IInteractionCommandContext>();
        ctx.Setup(c => c.Interaction).Returns(interaction.Object);

        return ctx.Object;
    }

    private static IFeedbackService CreateFeedbackService()
    {
        var feedback = new Mock<IFeedbackService>();
        feedback
            .Setup(f => f.SendContextualNeutralAsync(
                It.IsAny<string>(),
                It.IsAny<Snowflake?>(),
                It.IsAny<FeedbackMessageOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<IMessage>>.FromSuccess([]));
        return feedback.Object;
    }

    [Test]
    public async Task VoteBanAsync_AddsBannedVoterToDatabase()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();

        var targetUser = Mock.Of<IUser>(u =>
            u.ID == new Snowflake(9999L) &&
            u.Username == "target");

        var sut = new AdminCommands(
            CreateFeedbackService(),
            CreateAdminContext(),
            Mock.Of<IDiscordRestChannelAPI>(),
            dbContext,
            Mock.Of<ILogger<AdminCommands>>());

        await sut.VoteBanAsync(targetUser);

        dbContext.BannedVoters.Should().ContainSingle(v => v.UserId == 9999L);
    }

    [Test]
    public async Task VoteBanAsync_UserAlreadyBanned_DoesNotDuplicate()
    {
        var dbContext = TestHelpers.CreateVoteDbContext();
        dbContext.BannedVoters.Add(new BannedVoter { UserId = 9999L });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var targetUser = Mock.Of<IUser>(u =>
            u.ID == new Snowflake(9999L) &&
            u.Username == "target");

        var logger = new MockLogger<AdminCommands>();

        var sut = new AdminCommands(
            CreateFeedbackService(),
            CreateAdminContext(),
            Mock.Of<IDiscordRestChannelAPI>(),
            dbContext,
            logger);

        await sut.VoteBanAsync(targetUser);

        dbContext.BannedVoters.Should().ContainSingle();
        logger[LogLevel.Error].Should().Contain(message => message.Message.Contains("already banned"));
    }
}
