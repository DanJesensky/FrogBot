using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    public class TopChatCommand : IChatCommand
    {
        private readonly VoteDbContext _dbContext;
        private readonly IDiscordRestChannelAPI _channelApi;

        public TopChatCommand(VoteDbContext dbContext, IDiscordRestChannelAPI channelApi)
        {
            _dbContext = dbContext;
            _channelApi = channelApi;
        }

        public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
            messageCreateEvent.Content.Equals("!top");

        public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
        {
            if (messageCreateEvent.GuildID.HasValue)
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, "Sorry, I can only handle that in DMs for right now.");
                return Result.FromSuccess();
            }
            
            var topPoints = _dbContext.Votes.AsNoTracking()
                .GroupBy(v => v.ReceiverId)
                .Select(v => new { Id = v.Key, Total = v.Sum(vote => (int)vote.VoteType) })
                .OrderByDescending(v => v.Total)
                .Take(10);

            var index = 1;
            var sb = new StringBuilder();
            foreach (var top in topPoints)
            {
                sb.Append(index++).Append(". <@").Append(top.Id).Append(">: ").Append(top.Total).Append(" points").AppendLine();
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