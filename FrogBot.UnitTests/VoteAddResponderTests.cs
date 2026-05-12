using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.Responders;
using FrogBot.TikTok;
using FrogBot.Voting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.UnitTests;

public class VoteAddResponderTests
{
    private const ulong ServerId = 500UL;
    private const ulong VoterId = 100UL;
    private const ulong AuthorId = 200UL;
    private const ulong ChannelId = 300UL;
    private const ulong MessageId = 400UL;
    private const string UpvoteEmoji = "👍";
    private const string DownvoteEmoji = "👎";

    private static IUser CreateUser(ulong id, bool isBot = false)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.ID).Returns(new Snowflake(id));
        user.Setup(u => u.IsBot).Returns(new Optional<bool>(isBot));
        return user.Object;
    }

    private static IMessage CreateMessage(IUser author)
    {
        var message = new Mock<IMessage>();
        message.Setup(m => m.Author).Returns(author);
        message.Setup(m => m.ChannelID).Returns(new Snowflake(ChannelId));
        message.Setup(m => m.Mentions).Returns([]);
        message.Setup(m => m.Content).Returns(string.Empty);
        return message.Object;
    }

    private static IMessage CreateMessageWithBotReactions(IUser author)
    {
        var upvotePartialEmoji = new Mock<IPartialEmoji>();
        upvotePartialEmoji.Setup(e => e.Name).Returns(new Optional<string?>(UpvoteEmoji));

        var downvotePartialEmoji = new Mock<IPartialEmoji>();
        downvotePartialEmoji.Setup(e => e.Name).Returns(new Optional<string?>(DownvoteEmoji));

        var upvoteReaction = new Mock<IReaction>();
        upvoteReaction.Setup(r => r.Emoji).Returns(upvotePartialEmoji.Object);
        upvoteReaction.Setup(r => r.HasCurrentUserReacted).Returns(true);

        var downvoteReaction = new Mock<IReaction>();
        downvoteReaction.Setup(r => r.Emoji).Returns(downvotePartialEmoji.Object);
        downvoteReaction.Setup(r => r.HasCurrentUserReacted).Returns(true);

        IReadOnlyList<IReaction> reactions = [upvoteReaction.Object, downvoteReaction.Object];

        var message = new Mock<IMessage>();
        message.Setup(m => m.Author).Returns(author);
        message.Setup(m => m.ChannelID).Returns(new Snowflake(ChannelId));
        message.Setup(m => m.Mentions).Returns([]);
        message.Setup(m => m.Content).Returns(string.Empty);
        message.Setup(m => m.Reactions).Returns(new Optional<IReadOnlyList<IReaction>>(reactions));
        return message.Object;
    }

    private static IMessageReactionAdd CreateReactionEvent(
        IUser voter,
        ulong channelId = ChannelId,
        ulong messageId = MessageId,
        ulong guildId = ServerId)
    {
        var member = new Mock<IGuildMember>();
        member.Setup(m => m.User).Returns(new Optional<IUser>(voter));

        var emoji = Mock.Of<IPartialEmoji>();

        var evt = new Mock<IMessageReactionAdd>();
        evt.Setup(e => e.Member).Returns(new Optional<IGuildMember>(member.Object));
        evt.Setup(e => e.Emoji).Returns(emoji);
        evt.Setup(e => e.ChannelID).Returns(new Snowflake(channelId));
        evt.Setup(e => e.MessageID).Returns(new Snowflake(messageId));
        evt.Setup(e => e.UserID).Returns(voter.ID);
        evt.Setup(e => e.GuildID).Returns(new Optional<Snowflake>(new Snowflake(guildId)));
        return evt.Object;
    }

    private static IVoteEmojiProvider CreateVoteEmojiProvider(VoteType voteType = VoteType.Upvote)
    {
        var provider = new Mock<IVoteEmojiProvider>();
        provider.Setup(p => p.GetVoteTypeFromEmoji(It.IsAny<IPartialEmoji>())).Returns(voteType);
        provider.Setup(p => p.GetEmoji(VoteType.Upvote)).Returns(UpvoteEmoji);
        provider.Setup(p => p.GetEmoji(VoteType.Downvote)).Returns(DownvoteEmoji);
        return provider.Object;
    }

    private static ITikTokQuarantineManager CreateQuarantineManager(IUser author)
    {
        var quarantine = new Mock<ITikTokQuarantineManager>();
        quarantine.Setup(q => q.GetSubstituteQuarantineAuthor(It.IsAny<IMessage>())).Returns(author);
        return quarantine.Object;
    }

    private static Mock<IDiscordRestChannelAPI> CreateChannelApiMock()
    {
        var api = new Mock<IDiscordRestChannelAPI>();
        api.Setup(a => a.CreateReactionAsync(
                It.IsAny<Snowflake>(),
                It.IsAny<Snowflake>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.FromSuccess());
        return api;
    }

    private static VoteAddResponder CreateResponder(
        IVoteManager? voteManager = null,
        IMessageRetriever? messageRetriever = null,
        IVoteEmojiProvider? voteEmojiProvider = null,
        ITikTokQuarantineManager? quarantine = null,
        IDiscordRestChannelAPI? channelApi = null,
        ulong serverId = ServerId)
    {
        return new VoteAddResponder(
            Mock.Of<ILogger<VoteAddResponder>>(),
            Options.Create(new FrogBotOptions { ServerId = serverId }),
            voteManager ?? Mock.Of<IVoteManager>(),
            messageRetriever ?? Mock.Of<IMessageRetriever>(),
            voteEmojiProvider ?? CreateVoteEmojiProvider(),
            quarantine ?? Mock.Of<ITikTokQuarantineManager>(),
            channelApi ?? CreateChannelApiMock().Object);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task RespondAsync_ValidVote_AddsBothBotReactions(VoteType voteType)
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var voteManager = new Mock<IVoteManager>();
        voteManager.Setup(v => v.IsVoterBannedAsync(VoterId)).ReturnsAsync(false);

        var channelApi = CreateChannelApiMock();

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            voteEmojiProvider: CreateVoteEmojiProvider(voteType),
            quarantine: CreateQuarantineManager(author),
            channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            new Snowflake(ChannelId),
            new Snowflake(MessageId),
            UpvoteEmoji,
            It.IsAny<CancellationToken>()), Times.Once);

        channelApi.Verify(a => a.CreateReactionAsync(
            new Snowflake(ChannelId),
            new Snowflake(MessageId),
            DownvoteEmoji,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task RespondAsync_ValidVote_RecordsVote(VoteType voteType)
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var voteManager = new Mock<IVoteManager>();
        voteManager.Setup(v => v.IsVoterBannedAsync(VoterId)).ReturnsAsync(false);

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            voteEmojiProvider: CreateVoteEmojiProvider(voteType),
            quarantine: CreateQuarantineManager(author));

        await sut.RespondAsync(CreateReactionEvent(voter));

        voteManager.Verify(v => v.AddVoteAsync(ChannelId, MessageId, AuthorId, VoterId, voteType), Times.Once);
    }

    [Test]
    public async Task RespondAsync_BotVoter_DoesNotAddReactions()
    {
        var voter = CreateUser(VoterId, isBot: true);
        var channelApi = CreateChannelApiMock();
        var voteManager = new Mock<IVoteManager>();

        var sut = CreateResponder(voteManager: voteManager.Object, channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            It.IsAny<Snowflake>(),
            It.IsAny<Snowflake>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        voteManager.Verify(v => v.AddVoteAsync(
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    public async Task RespondAsync_BotAuthor_DoesNotAddReactions()
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId, isBot: true);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var channelApi = CreateChannelApiMock();
        var voteManager = new Mock<IVoteManager>();

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            quarantine: CreateQuarantineManager(author),
            channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            It.IsAny<Snowflake>(),
            It.IsAny<Snowflake>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        voteManager.Verify(v => v.AddVoteAsync(
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    public async Task RespondAsync_SelfVote_DoesNotAddReactions()
    {
        var voter = CreateUser(VoterId);
        // Author has same ID as voter
        var author = CreateUser(VoterId);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var channelApi = CreateChannelApiMock();
        var voteManager = new Mock<IVoteManager>();

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            quarantine: CreateQuarantineManager(author),
            channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            It.IsAny<Snowflake>(),
            It.IsAny<Snowflake>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        voteManager.Verify(v => v.AddVoteAsync(
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    public async Task RespondAsync_BannedVoter_DoesNotAddReactions()
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var voteManager = new Mock<IVoteManager>();
        voteManager.Setup(v => v.IsVoterBannedAsync(VoterId)).ReturnsAsync(true);

        var channelApi = CreateChannelApiMock();

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            quarantine: CreateQuarantineManager(author),
            channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            It.IsAny<Snowflake>(),
            It.IsAny<Snowflake>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RespondAsync_BannedVoter_DoesNotRecordVote()
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId);
        var message = CreateMessage(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var voteManager = new Mock<IVoteManager>();
        voteManager.Setup(v => v.IsVoterBannedAsync(VoterId)).ReturnsAsync(true);

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            quarantine: CreateQuarantineManager(author));

        await sut.RespondAsync(CreateReactionEvent(voter));

        voteManager.Verify(v => v.AddVoteAsync(
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<ulong>(),
            It.IsAny<VoteType>()), Times.Never);
    }

    [Test]
    [TestCase(VoteType.Upvote)]
    [TestCase(VoteType.Downvote)]
    public async Task RespondAsync_BotReactionsAlreadyPresent_DoesNotAddReactions(VoteType voteType)
    {
        var voter = CreateUser(VoterId);
        var author = CreateUser(AuthorId);
        var message = CreateMessageWithBotReactions(author);

        var messageRetriever = new Mock<IMessageRetriever>();
        messageRetriever.Setup(r => r.RetrieveMessageAsync(It.IsAny<Snowflake>(), It.IsAny<Snowflake>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var voteManager = new Mock<IVoteManager>();
        voteManager.Setup(v => v.IsVoterBannedAsync(VoterId)).ReturnsAsync(false);

        var channelApi = CreateChannelApiMock();

        var sut = CreateResponder(
            voteManager: voteManager.Object,
            messageRetriever: messageRetriever.Object,
            voteEmojiProvider: CreateVoteEmojiProvider(voteType),
            quarantine: CreateQuarantineManager(author),
            channelApi: channelApi.Object);

        await sut.RespondAsync(CreateReactionEvent(voter));

        channelApi.Verify(a => a.CreateReactionAsync(
            It.IsAny<Snowflake>(),
            It.IsAny<Snowflake>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
