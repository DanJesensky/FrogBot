using System.Runtime.CompilerServices;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting
{
    public class VoteEmojiProvider : IVoteEmojiProvider
    {
        internal const string UpvoteEmoji = "dan:425499646752981003";
        internal const string DownvoteEmoji = "jim:425500620212928547";
        private const ulong UpvoteEmojiId = 425499646752981003L;
        private const ulong DownvoteEmojiId = 425500620212928547L;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public string? GetEmoji(VoteType type)
        {
            return type switch
            {
                VoteType.Upvote => UpvoteEmoji,
                VoteType.Downvote => DownvoteEmoji,
                _ => null
            };
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