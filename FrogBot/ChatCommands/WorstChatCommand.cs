using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

public class WorstChatCommand(
    VoteDbContext dbContext,
    IDiscordRestChannelAPI channelApi,
    IUsernameCachingService usernameCache)
    : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!worst");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var channel = await channelApi.GetChannelAsync(message.ChannelID);
        var isGuildMessage = channel.Entity.GuildID.HasValue;

        // Default to 1, allow a requested maximum of 10
        var requestedCount = message.Content.Split(' ', 2).Last();
        _ = int.TryParse(requestedCount, out var parsedRequestedCount);
        var count = Math.Max(1, Math.Min(parsedRequestedCount, 10));

        var topPoints = await dbContext.Votes.AsNoTracking()
            .GroupBy(v => v.ReceiverId)
            .Select(v => new { Id = v.Key, Total = v.Sum(vote => (int)vote.VoteType) })
            .OrderBy(v => v.Total)
            .Take(count)
            .ToArrayAsync();

        var index = await dbContext.Votes
            .Select(v => v.ReceiverId)
            .Distinct()
            .CountAsync();

        var sb = new StringBuilder();
        foreach (var top in topPoints)
        {
            if (isGuildMessage)
            {
                var username = await usernameCache.GetCachedUsernameAsync(top.Id);
                sb.Append(index--).Append(". ").Append(username).Append(": ").Append(top.Total).Append(" points").AppendLine();
            }
            else
            {
                sb.Append(index--).Append(". <@").Append(top.Id).Append(">: ").Append(top.Total).Append(" points").AppendLine();
            }
        }

        if (sb.Length == 0)
        {
            await channelApi.CreateMessageAsync(message.ChannelID, "There are no eligible users to display.");
        }
        else
        {
            await channelApi.CreateMessageAsync(message.ChannelID, sb.ToString());
        }
        return Result.FromSuccess();
    }
}