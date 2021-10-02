using System.Threading;
using System.Threading.Tasks;
using FrogBot.Voting;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders
{
    public class VoteRemoveResponder : IResponder<IMessageReactionRemove>
    {
        private readonly IVoteManager _voteManager;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IVoteEmojiProvider _voteEmojiProvider;

        public VoteRemoveResponder(IVoteManager voteManager, IDiscordRestChannelAPI channelApi, IVoteEmojiProvider voteEmojiProvider)
        {
            _voteManager = voteManager;
            _channelApi = channelApi;
            _voteEmojiProvider = voteEmojiProvider;
        }

        public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
        {
            var voteType = _voteEmojiProvider.GetVoteTypeFromEmoji(gatewayEvent.Emoji);
            if (voteType is null)
            {
                return Result.FromSuccess();
            }

            var message = await _channelApi.GetChannelMessageAsync(gatewayEvent.ChannelID, gatewayEvent.MessageID, ct);
            if (!message.IsSuccess)
            {
                await _voteManager.RemoveAllVotesAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value);
                return Result.FromError(message.Error);
            }

            await _voteManager.RemoveVoteAsync(gatewayEvent.ChannelID.Value, gatewayEvent.MessageID.Value, message.Entity.Author.ID.Value,
                gatewayEvent.UserID.Value, voteType.Value);

            return Result.FromSuccess();
        }
    }
}