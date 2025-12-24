using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.ChatCommands;
using FrogBot.ChatCommands.Authorization;
using JetBrains.Annotations;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.Responders;

[UsedImplicitly]
public class ChatCommandResponder(IEnumerable<IChatCommand> commands, IDiscordRestChannelAPI channelApi)
    : IChatResponder
{
    public async Task<Result> RespondAsync(IMessage message, CancellationToken cancellation = default)
    {
        if (!message.Content.StartsWith('!'))
        {
            return Result.FromSuccess();
        }

        var matchingCommand = commands.FirstOrDefault(command => command.CanHandleCommand(message));
        if (matchingCommand == null)
        {
            return Result.FromSuccess();
        }

        var authzAttribute = matchingCommand.GetType().GetCustomAttribute<ChatCommandAuthorizationAttribute>();
        if (authzAttribute?.IsAuthorized(message.Author, message) == false)
        {
            await channelApi.CreateMessageAsync(message.ChannelID, "Sorry, you're not allowed to do that.", ct: cancellation);
            return Result.FromSuccess();
        }

        await matchingCommand.HandleCommandAsync(message);
        return Result.FromSuccess();
    }
}