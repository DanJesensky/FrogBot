using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace FrogBot;

public interface IMessageRetriever
{
    Task<IMessage?> RetrieveMessageAsync(Snowflake channelId, Snowflake messageId, CancellationToken cancellation = default);
}