using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Results;

namespace FrogBot.Responders;

public interface IChatResponder
{
    Task<Result> RespondAsync(IMessage message, IMessageCreate messageEvent, CancellationToken cancellation = default);
}