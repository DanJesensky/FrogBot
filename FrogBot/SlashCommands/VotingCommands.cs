using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace FrogBot.SlashCommands;

[UsedImplicitly]
public class VotingCommands(
    VoteDbContext dbContext,
    IFeedbackService feedback,
    ICommandContext context,
    IUsernameCachingService usernameCache,
    IVoteManager voteManager,
    IOptionsSnapshot<FrogBotOptions> botOptions)
    : CommandGroup
{
    private readonly bool _useMentions = botOptions.Value.AlwaysUseMentions;

    [Command("top")]
    [Description("Show users with the highest vote points")]
    public async Task<IResult> TopAsync([Description("Number of users to show (1-10)")] int count = 10)
    {
        var isGuildMessage = context is IInteractionCommandContext { Interaction.GuildID.HasValue: true };

        count = Math.Max(1, Math.Min(count, 10));

        var topPoints = await dbContext.AdjustedVotes.AsNoTracking()
            .GroupBy(v => v.ReceiverId)
            .Select(v => new { Id = v.Key, Total = v.Sum(vote => (int)vote.VoteType) })
            .OrderByDescending(v => v.Total)
            .Take(count)
            .ToArrayAsync(CancellationToken);

        if (topPoints.Length == 0)
        {
            return await feedback.SendContextualNeutralAsync("There are no eligible users to display.", ct: CancellationToken);
        }

        var index = 1;
        var sb = new StringBuilder();
        foreach (var entry in topPoints)
        {
            if (isGuildMessage || _useMentions)
            {
                var username = await usernameCache.GetCachedUsernameAsync(entry.Id);
                sb.Append(index++).Append("\\. ").Append(username).Append(": ").Append(entry.Total).Append(" points").AppendLine();
            }
            else
            {
                sb.Append(index++).Append("\\. <@").Append(entry.Id).Append(">: ").Append(entry.Total).Append(" points").AppendLine();
            }
        }

        return await feedback.SendContextualSuccessAsync(sb.ToString(), ct: CancellationToken);
    }

    [Command("worst")]
    [Description("Show users with the lowest vote points")]
    public async Task<IResult> WorstAsync(
        [Description("Number of users to show (1-10)")] int count = 1)
    {
        var isGuildMessage = context is IInteractionCommandContext { Interaction.GuildID.HasValue: true };

        count = Math.Max(1, Math.Min(count, 10));

        var worstPoints = await dbContext.AdjustedVotes.AsNoTracking()
            .GroupBy(v => v.ReceiverId)
            .Select(v => new { Id = v.Key, Total = v.Sum(vote => (int)vote.VoteType) })
            .OrderBy(v => v.Total)
            .Take(count)
            .ToArrayAsync(CancellationToken);

        var totalUsers = await dbContext.Votes
            .Select(v => v.ReceiverId)
            .Distinct()
            .CountAsync(CancellationToken);

        if (worstPoints.Length == 0)
        {
            return await feedback.SendContextualNeutralAsync("There are no eligible users to display.", ct: CancellationToken);
        }

        var index = totalUsers;
        var sb = new StringBuilder();
        foreach (var entry in worstPoints)
        {
            if (isGuildMessage || _useMentions)
            {
                var username = await usernameCache.GetCachedUsernameAsync(entry.Id);
                sb.Append(index--).Append("\\. ").Append(username).Append(": ").Append(entry.Total).Append(" points").AppendLine();
            }
            else
            {
                sb.Append(index--).Append("\\. <@").Append(entry.Id).Append(">: ").Append(entry.Total).Append(" points").AppendLine();
            }
        }

        return await feedback.SendContextualSuccessAsync(sb.ToString(), ct: CancellationToken);
    }

    [Command("points")]
    [Description("Show vote points for a user")]
    public async Task<IResult> PointsAsync(
        [Description("User to check (defaults to yourself)")] IUser? user = null)
    {
        ulong targetId;
        if (user is not null)
        {
            targetId = user.ID.Value;
        }
        else if (context is IInteractionCommandContext ic)
        {
            targetId = ic.Interaction.Member is { HasValue: true, Value.User.HasValue: true }
                ? ic.Interaction.Member.Value.User.Value.ID.Value
                : ic.Interaction.User.HasValue ? ic.Interaction.User.Value.ID.Value : 0;
        }
        else
        {
            return Result.FromError(new InvalidOperationError("Could not determine the invoking user."));
        }

        var votes = await voteManager.GetScoreAsync(targetId);

        string username;
        if (_useMentions)
        {
            username = $"<@{targetId}>";
        }
        else
        {
            username = await usernameCache.GetCachedUsernameAsync(targetId);
        }

        return await feedback.SendContextualSuccessAsync($"{username}: {votes} points", ct: CancellationToken);
    }
}
