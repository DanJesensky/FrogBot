using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.ChatCommands;
using FrogBot.ChatCommands.Authorization;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders
{
    public class ChatCommandResponder : IResponder<IMessageCreate>
    {
        private readonly IEnumerable<IChatCommand> _chatCommands;
        private readonly IDiscordRestChannelAPI _channelApi;

        public ChatCommandResponder(IEnumerable<IChatCommand> commands, IDiscordRestChannelAPI channelApi)
        {
            _chatCommands = commands;
            _channelApi = channelApi;
        }
        
        public async Task<Result> RespondAsync(IMessageCreate messageCreateEvent, CancellationToken ct = default)
        {
            if (!messageCreateEvent.Content.StartsWith("!"))
            {
                return Result.FromSuccess();
            }

            var matchingCommand = _chatCommands.FirstOrDefault(command => command.CanHandleCommand(messageCreateEvent));
            if (matchingCommand == null)
            {
                return Result.FromSuccess();
            }

            var authzAttribute = matchingCommand.GetType().GetCustomAttribute<ChatCommandAuthorizationAttribute>();
            if (authzAttribute?.IsAuthorized(messageCreateEvent.Author, messageCreateEvent.ReferencedMessage.Value) != true)
            {
                await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, "Sorry, you're not allowed to do that.");
                return Result.FromSuccess();
            }

            await matchingCommand.HandleCommandAsync(messageCreateEvent);
            return Result.FromSuccess();
        }
    }
}