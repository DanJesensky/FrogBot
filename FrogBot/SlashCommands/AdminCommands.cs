using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FrogBot.Voting;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace FrogBot.SlashCommands;

[UsedImplicitly]
public partial class AdminCommands(
    IFeedbackService feedback,
    ICommandContext context,
    IDiscordRestChannelAPI channelApi,
    VoteDbContext voteDbContext,
    ILogger<AdminCommands> logger)
    : CommandGroup
{
    private const ulong AdminUserId = 159870805390524416;

    private static readonly Regex EmojiRegex = GenerateEmojiRegex();

    [Command("test")]
    [Description("Test that the bot is responding")]
    public async Task<IResult> TestAsync()
    {
        if (!IsAdmin())
        {
            return await SendAdminError();
        }

        return await feedback.SendContextualSuccessAsync("✅", ct: CancellationToken);
    }

    [Command("say")]
    [Description("Send a message to another channel")]
    public async Task<IResult> SayAsync(
        [Description("Channel to send the message to")] Snowflake channel,
        [Description("Message content")] string message)
    {
        if (!IsAdmin())
        {
            return await SendAdminError();
        }

        await channelApi.CreateMessageAsync(channel, message, ct: CancellationToken);
        return await feedback.SendContextualSuccessAsync("✅", ct: CancellationToken);
    }

    [Command("emojiid")]
    [Description("Extract custom emoji IDs from text")]
    public async Task<IResult> EmojiIdAsync(
        [Description("Text containing custom emojis")] string text)
    {
        if (!IsAdmin())
        {
            return await SendAdminError();
        }

        var matches = EmojiRegex.Matches(text);
        var sb = new StringBuilder();
        foreach (var matchGroups in matches.Select(m => m.Groups))
        {
            sb.Append(matchGroups["fullEmoji"]).Append(' ').Append(matchGroups["id"]).AppendLine();
        }

        var response = sb.Length > 0 ? sb.ToString() : "No custom emojis found.";
        return await feedback.SendContextualNeutralAsync(response, ct: CancellationToken);
    }

    [Command("voteban")]
    [Description("Ban a user from the voting system")]
    public async Task<IResult> VoteBanAsync([Description("User to ban from voting")] IUser user)
    {
        if (!IsAdmin())
        {
            return await SendAdminError();
        }

        var bannedUser = new BannedVoter { UserId = user.ID.Value };
        if (await voteDbContext.BannedVoters.ContainsAsync(bannedUser, CancellationToken))
        {
            logger.LogError("Cannot ban {username} ({id}) from voting: they are already banned.", user.Username, user.ID.Value);
            return await feedback.SendContextualErrorAsync($"❌ {user.Username} is already banned from voting.", ct: CancellationToken);
        }

        try
        {
            voteDbContext.BannedVoters.Add(bannedUser);
            await voteDbContext.SaveChangesAsync(CancellationToken);
            logger.LogInformation("{username} ({id}) was banned from voting.", user.Username, user.ID.Value);
            return await feedback.SendContextualSuccessAsync($"✅ {user.Username} has been banned from voting.", ct: CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save banned user to database");
            return await feedback.SendContextualErrorAsync("❌ Failed to ban user from voting.", ct: CancellationToken);
        }
    }

    [Command("voteunban")]
    [Description("Unban a user from the voting system")]
    public async Task<IResult> VoteUnbanAsync([Description("User to unban from voting")] IUser user)
    {
        if (!IsAdmin())
        {
            return await SendAdminError();
        }

        var bannedUser = new BannedVoter { UserId = user.ID.Value };
        if (!await voteDbContext.BannedVoters.ContainsAsync(bannedUser, CancellationToken))
        {
            logger.LogError("Cannot unban {username} ({id}) from voting: they are not banned.", user.Username, user.ID.Value);
            return await feedback.SendContextualErrorAsync($"❌ {user.Username} is not banned from voting.", ct: CancellationToken);
        }

        try
        {
            voteDbContext.BannedVoters.Remove(bannedUser);
            await voteDbContext.SaveChangesAsync(CancellationToken);
            logger.LogInformation("{username} ({id}) was unbanned from voting.", user.Username, user.ID.Value);
            return await feedback.SendContextualSuccessAsync($"✅ {user.Username} has been unbanned from voting.", ct: CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove banned user from database");
            return await feedback.SendContextualErrorAsync("❌ Failed to unban user from voting.", ct: CancellationToken);
        }
    }

    private bool IsAdmin()
    {
        if (context is not IInteractionCommandContext ic)
        {
            return false;
        }

        var userId = ic.Interaction.Member is { HasValue: true, Value.User.HasValue: true }
            ? ic.Interaction.Member.Value.User.Value.ID.Value
            : ic.Interaction.User.HasValue ? ic.Interaction.User.Value.ID.Value : 0UL;

        return userId == AdminUserId;
    }
    
    private async Task<IResult> SendAdminError() =>
        await feedback.SendContextualAsync("Sorry, you're not allowed to do that.", ct: CancellationToken);

    [GeneratedRegex("(?<fullEmoji><:(?:[^:]+):(?<id>\\d+)>)", RegexOptions.Compiled)]
    private static partial Regex GenerateEmojiRegex();
}
