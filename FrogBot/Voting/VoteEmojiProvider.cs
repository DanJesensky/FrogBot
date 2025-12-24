using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Voting;

public class VoteEmojiProvider(IOptions<VoteOptions> voteOptions) : IVoteEmojiProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string? GetEmoji(VoteType type)
    {
        return type switch
        {
            VoteType.Upvote => voteOptions.Value.BotUpvoteEmoji,
            VoteType.Downvote => voteOptions.Value.BotDownvoteEmoji,
            _ => null
        };
    }

    public VoteType? GetVoteTypeFromEmoji(IPartialEmoji emoji)
    {
        var options = voteOptions.Value;
        string? emojiId;

        // why is this even possible?
        if (emoji.ID.HasValue && emoji.ID.Value != null)
        {
            emojiId = emoji.ID.Value!.Value.Value.ToString();
        }
        else if (emoji.Name.HasValue)
        {
            emojiId = emoji.Name.Value;
        }
        else
        {
            return null;
        }

        if (options.UpvoteEmojis.Contains(emojiId))
        {
            return VoteType.Upvote;
        }

        if (options.DownvoteEmojis.Contains(emojiId))
        {
            return VoteType.Downvote;
        }

        return null;
    }
}