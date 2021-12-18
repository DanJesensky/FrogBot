using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Results;

namespace FrogBot.ChatCommands;

public interface IChatCommand
{
    bool CanHandleCommand(IMessageCreate messageCreateEvent);

    Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent);
}