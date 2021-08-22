using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting
{
    public interface IVoteEmojiProvider
    {
        IEmoji GetEmoji(VoteType type);

        VoteType? GetVoteTypeFromEmoji(IPartialEmoji emoji);
    }
}