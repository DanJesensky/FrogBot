using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[UsedImplicitly]
public class TopChatCommand(
    VoteDbContext dbContext,
    IDiscordRestChannelAPI channelApi,
    IUsernameCachingService usernameCache)
    : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.Equals("!top");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var channel = await channelApi.GetChannelAsync(message.ChannelID);
        var isGuildMessage = channel.Entity.GuildID.HasValue;
        var topPoints = await dbContext.Votes.AsNoTracking()
            .GroupBy(v => v.ReceiverId)
            .Select(v => new { Id = v.Key, Total = v.Sum(vote => (int)vote.VoteType) })
            .OrderByDescending(v => v.Total)
            .Take(10)
            .ToArrayAsync();

        var index = 1;
        var sb = new StringBuilder();
        foreach (var top in topPoints)
        {
            if (isGuildMessage)
            {
                var username = await usernameCache.GetCachedUsernameAsync(top.Id);
                sb.Append(index++).Append(". ").Append(username).Append(": ").Append(top.Total).Append(" points").AppendLine();
            }
            else
            {
                sb.Append(index++).Append(". <@").Append(top.Id).Append(">: ").Append(top.Total).Append(" points").AppendLine();
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