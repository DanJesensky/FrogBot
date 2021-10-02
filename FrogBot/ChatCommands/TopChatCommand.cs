using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    public class TopChatCommand : IChatCommand
    {
        private readonly VoteDbContext _dbContext;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IUsernameCachingService _usernameCache;

        public TopChatCommand(VoteDbContext dbContext, IDiscordRestChannelAPI channelApi, IUsernameCachingService usernameCache)
        {
            _dbContext = dbContext;
            _channelApi = channelApi;
            _usernameCache = usernameCache;
        }

        public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
            messageCreateEvent.Content.Equals("!top");

        public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
        {
            bool isGuildMessage = messageCreateEvent.GuildID.HasValue;
            var topPoints = await _dbContext.Votes.AsNoTracking()
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
                    var username = await _usernameCache.GetCachedUsernameAsync(top.Id);
                    sb.Append(index++).Append(". ").Append(username).Append(": ").Append(top.Total).Append(" points").AppendLine();
                }
                else
                {
                    sb.Append(index++).Append(". <@").Append(top.Id).Append(">: ").Append(top.Total).Append(" points").AppendLine();   
                }
            }

            if (sb.Length == 0)
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, "There are no eligible users to display.");
            }
            else
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, sb.ToString());   
            }
            return Result.FromSuccess();
        }
    }
}