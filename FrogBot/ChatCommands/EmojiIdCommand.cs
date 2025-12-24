using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
[UsedImplicitly]
public partial class EmojiIdCommand(IDiscordRestChannelAPI channelApi) : IChatCommand
{
    private static readonly Regex EmojiRegex = GenerateEmojiRegex();

    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!emojiId", StringComparison.OrdinalIgnoreCase);

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var matches = EmojiRegex.Matches(message.Content);

        StringBuilder sb = new();
        foreach (var matchGroups in matches.Select(match => match.Groups))
        {
            sb.Append(matchGroups["fullEmoji"]).Append(' ').Append(matchGroups["id"]);
        }

        await channelApi.CreateMessageAsync(message.ChannelID, sb.ToString());

        return Result.FromSuccess();
    }

    [GeneratedRegex("(?<fullEmoji><:(?:[^:]+):(?<id>\\d+)>)", RegexOptions.Compiled)]
    private static partial Regex GenerateEmojiRegex();
}