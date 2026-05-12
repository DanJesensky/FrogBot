using System;
using System.Threading.Tasks;
using FluentAssertions;
using FrogBot.Responders;
using FrogBot.TikTok;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace FrogBot.UnitTests;

public class VoteAgeResponderTests
{
    [Test]
    public async Task VoteAddResponder_OldMessage_DoesNotRecordVote()
    {
        var voteManager = new Mock<IVoteManager>();
        var messageRetriever = new Mock<IMessageRetriever>();
        var voteEmojiProvider = new Mock<IVoteEmojiProvider>();
        var quarantine = new Mock<ITikTokQuarantineManager>();
        var @event = CreateAddEvent();
        var message = Mock.Of<IMessage>(m => m.Timestamp == DateTimeOffset.UtcNow - TimeSpan.FromDays(31));

        voteEmojiProvider.Setup(v => v.GetVoteTypeFromEmoji(It.IsAny<IPartialEmoji>())).Returns(VoteType.Upvote);
        messageRetriever.Setup(r => r.RetrieveMessageAsync(@event.ChannelID, @event.MessageID, default)).ReturnsAsync(message);

        var sut = new VoteAddResponder(
            Mock.Of<ILogger<VoteAddResponder>>(),
            Options.Create(new FrogBotOptions { ServerId = 1 }),
            Options.Create(new VoteOptions { MaximumMessageAge = TimeSpan.FromDays(30) }),
            voteManager.Object,
            messageRetriever.Object,
            voteEmojiProvider.Object,
            quarantine.Object);

        var result = await sut.RespondAsync(@event);

        result.IsSuccess.Should().BeTrue();
        voteManager.Verify(v => v.AddVoteAsync(It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    public async Task VoteAddResponder_RecentMessage_RecordsVote()
    {
        var voteManager = new Mock<IVoteManager>();
        var messageRetriever = new Mock<IMessageRetriever>();
        var voteEmojiProvider = new Mock<IVoteEmojiProvider>();
        var quarantine = new Mock<ITikTokQuarantineManager>();
        var @event = CreateAddEvent();

        var author = Mock.Of<IUser>(u =>
            u.ID == new Snowflake(3) &&
            u.IsBot == new Optional<bool>(false));
        var message = Mock.Of<IMessage>(m => m.Timestamp == DateTimeOffset.UtcNow - TimeSpan.FromDays(1));

        voteEmojiProvider.Setup(v => v.GetVoteTypeFromEmoji(It.IsAny<IPartialEmoji>())).Returns(VoteType.Upvote);
        messageRetriever.Setup(r => r.RetrieveMessageAsync(@event.ChannelID, @event.MessageID, default)).ReturnsAsync(message);
        quarantine.Setup(q => q.GetSubstituteQuarantineAuthor(message)).Returns(author);

        var sut = new VoteAddResponder(
            Mock.Of<ILogger<VoteAddResponder>>(),
            Options.Create(new FrogBotOptions { ServerId = 1 }),
            Options.Create(new VoteOptions { MaximumMessageAge = TimeSpan.FromDays(30) }),
            voteManager.Object,
            messageRetriever.Object,
            voteEmojiProvider.Object,
            quarantine.Object);

        var result = await sut.RespondAsync(@event);

        result.IsSuccess.Should().BeTrue();
        voteManager.Verify(v => v.AddVoteAsync(1, 2, 3, 4, VoteType.Upvote), Times.Once);
    }

    [Test]
    public async Task VoteRemoveResponder_OldMessage_DoesNotRemoveVote()
    {
        var voteManager = new Mock<IVoteManager>();
        var messageRetriever = new Mock<IMessageRetriever>();
        var voteEmojiProvider = new Mock<IVoteEmojiProvider>();
        var quarantine = new Mock<ITikTokQuarantineManager>();
        var @event = CreateRemoveEvent();
        var message = Mock.Of<IMessage>(m => m.Timestamp == DateTimeOffset.UtcNow - TimeSpan.FromDays(31));

        voteEmojiProvider.Setup(v => v.GetVoteTypeFromEmoji(It.IsAny<IPartialEmoji>())).Returns(VoteType.Upvote);
        messageRetriever.Setup(r => r.RetrieveMessageAsync(@event.ChannelID, @event.MessageID, default)).ReturnsAsync(message);

        var sut = new VoteRemoveResponder(
            voteManager.Object,
            messageRetriever.Object,
            voteEmojiProvider.Object,
            Options.Create(new VoteOptions { MaximumMessageAge = TimeSpan.FromDays(30) }),
            quarantine.Object);

        var result = await sut.RespondAsync(@event);

        result.IsSuccess.Should().BeTrue();
        voteManager.Verify(v => v.RemoveVoteAsync(It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    public async Task VoteRemoveResponder_RecentMessage_RemovesVote()
    {
        var voteManager = new Mock<IVoteManager>();
        var messageRetriever = new Mock<IMessageRetriever>();
        var voteEmojiProvider = new Mock<IVoteEmojiProvider>();
        var quarantine = new Mock<ITikTokQuarantineManager>();
        var @event = CreateRemoveEvent();

        var author = Mock.Of<IUser>(u =>
            u.ID == new Snowflake(3) &&
            u.IsBot == new Optional<bool>(false));
        var message = Mock.Of<IMessage>(m => m.Timestamp == DateTimeOffset.UtcNow - TimeSpan.FromDays(1));

        voteEmojiProvider.Setup(v => v.GetVoteTypeFromEmoji(It.IsAny<IPartialEmoji>())).Returns(VoteType.Upvote);
        messageRetriever.Setup(r => r.RetrieveMessageAsync(@event.ChannelID, @event.MessageID, default)).ReturnsAsync(message);
        quarantine.Setup(q => q.GetSubstituteQuarantineAuthor(message)).Returns(author);

        var sut = new VoteRemoveResponder(
            voteManager.Object,
            messageRetriever.Object,
            voteEmojiProvider.Object,
            Options.Create(new VoteOptions { MaximumMessageAge = TimeSpan.FromDays(30) }),
            quarantine.Object);

        var result = await sut.RespondAsync(@event);

        result.IsSuccess.Should().BeTrue();
        voteManager.Verify(v => v.RemoveVoteAsync(1, 2, 3, 4, VoteType.Upvote), Times.Once);
    }

    private static IMessageReactionAdd CreateAddEvent()
    {
        var voter = new Mock<IUser>();
        voter.SetupGet(u => u.IsBot).Returns(new Optional<bool>(false));
        voter.SetupGet(u => u.ID).Returns(new Snowflake(4));

        var member = new Mock<IGuildMember>();
        member.SetupGet(m => m.User).Returns(new Optional<IUser>(voter.Object));

        var messageReactionAdd = new Mock<IMessageReactionAdd>();
        messageReactionAdd.SetupGet(e => e.ChannelID).Returns(new Snowflake(1));
        messageReactionAdd.SetupGet(e => e.MessageID).Returns(new Snowflake(2));
        messageReactionAdd.SetupGet(e => e.UserID).Returns(new Snowflake(4));
        messageReactionAdd.SetupGet(e => e.GuildID).Returns(new Optional<Snowflake>(new Snowflake(1)));
        messageReactionAdd.SetupGet(e => e.Member).Returns(new Optional<IGuildMember>(member.Object));
        messageReactionAdd.SetupGet(e => e.Emoji).Returns(Mock.Of<IPartialEmoji>());

        return messageReactionAdd.Object;
    }

    private static IMessageReactionRemove CreateRemoveEvent()
    {
        var messageReactionRemove = new Mock<IMessageReactionRemove>();
        messageReactionRemove.SetupGet(e => e.ChannelID).Returns(new Snowflake(1));
        messageReactionRemove.SetupGet(e => e.MessageID).Returns(new Snowflake(2));
        messageReactionRemove.SetupGet(e => e.UserID).Returns(new Snowflake(4));
        messageReactionRemove.SetupGet(e => e.Emoji).Returns(Mock.Of<IPartialEmoji>());
        return messageReactionRemove.Object;
    }
}
