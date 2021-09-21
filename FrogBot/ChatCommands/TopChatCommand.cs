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

            await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, sb.ToString());
            return Result.FromSuccess();
        }
    }
}