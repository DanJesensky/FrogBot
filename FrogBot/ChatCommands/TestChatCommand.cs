using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    public class TestChatCommand : IChatCommand
    {
        private readonly IDiscordRestChannelAPI _channelApi;

        public TestChatCommand(IDiscordRestChannelAPI channelApi)
        {
            _channelApi = channelApi;
        }
        
        public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
            messageCreateEvent.Content.StartsWith("!test");

        public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
        {
            await _channelApi.CreateReactionAsync(messageCreateEvent.ChannelID, messageCreateEvent.ID, "👍");
            return Result.FromSuccess();
        }
    }
}