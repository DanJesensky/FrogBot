using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting
{
    public interface IVoteEmojiProvider
    {
        string? GetEmoji(VoteType type);

        VoteType? GetVoteTypeFromEmoji(IPartialEmoji emoji);
    }
}