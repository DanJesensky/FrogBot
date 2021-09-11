using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting
{
    public class VoteEmojiProvider : IVoteEmojiProvider
    {
        internal const ulong UpvoteEmojiId = 425499646752981003L;
        internal const ulong DownvoteEmojiId = 425500620212928547L;

        public IEmoji GetEmoji(VoteType type)
        {
            throw new System.NotImplementedException();
        }

        public VoteType? GetVoteTypeFromEmoji(IPartialEmoji emoji)
        {
            // why is this even possible?
            if (emoji.ID.HasValue && emoji.ID.Value != null)
            {
                return emoji.ID.Value!.Value.Value switch
                {
                    UpvoteEmojiId => VoteType.Upvote,
                    DownvoteEmojiId => VoteType.Downvote,
                    _ => null
                };
            }

            return emoji.Name.HasValue
                ? emoji.Name.Value switch
                {
                    "⬆" => VoteType.Upvote,
                    "⬇" => VoteType.Downvote,
                    _ => null
                }
                : null;
        }
    }
}