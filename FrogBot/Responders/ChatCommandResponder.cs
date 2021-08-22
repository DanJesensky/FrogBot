using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.ChatCommands;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace FrogBot.Responders
{
    public class ChatCommandResponder : IResponder<IMessageCreate>
    {
        private readonly IEnumerable<IChatCommand> _chatCommands;
            
        public ChatCommandResponder(IEnumerable<IChatCommand> commands)
        {
            _chatCommands = commands;
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

            await matchingCommand.HandleCommandAsync(messageCreateEvent);
            return Result.FromSuccess();
        }
    }
}