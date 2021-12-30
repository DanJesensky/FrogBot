using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Results;

namespace FrogBot.ChatCommands;

public interface IChatCommand
{
    bool CanHandleCommand(IMessage message);

    Task<Result> HandleCommandAsync(IMessage message);
}