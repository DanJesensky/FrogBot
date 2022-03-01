using System;
using System.Diagnostics.CodeAnalysis;

namespace FrogBot.Voting;

[ExcludeFromCodeCoverage]
public class VoteOptions
{
    public string[] UpvoteEmojis { get; set; } = Array.Empty<string>();
    public string[] DownvoteEmojis { get; set; } = Array.Empty<string>();

    public string BotUpvoteEmoji { get; set; } = null!;
    public string BotDownvoteEmoji { get; set; } = null!;
}