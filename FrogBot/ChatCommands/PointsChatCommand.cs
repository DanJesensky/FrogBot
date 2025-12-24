using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogBot.Voting;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[UsedImplicitly]
public class PointsChatCommand(IDiscordRestChannelAPI channelApi, IVoteManager voteManager)
    : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        // ReSharper disable once StringLiteralTypo
        message.Content.StartsWith("!points") || message.Content.StartsWith("!pointys");

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        IEnumerable<ulong> targets;
        if (message.Mentions.Count > 0)
        {
            if (message.Mentions.Count > 5)
            {
                return Result.FromError(new InvalidOperationError("Only up to five users may be specified at once."));
            }

            targets = message.Mentions.Select(user => user.ID.Value);
        }
        else
        {
            targets = new[] { message.Author.ID.Value };
        }

        var sb = new StringBuilder();
        foreach (var target in targets)
        {
            var votes = await voteManager.GetScoreAsync(target);
            sb.Append("<@").Append(target).Append(">: ").Append(votes).Append(" points").AppendLine();
        }

        await channelApi.CreateMessageAsync(message.ChannelID, sb.ToString());
        return Result.FromSuccess();
    }
}