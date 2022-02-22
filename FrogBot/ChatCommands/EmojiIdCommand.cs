using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
public class EmojiIdCommand : IChatCommand
{
    private static readonly Regex _emojiRegex = new("(?<fullEmoji><:(?:[^:]+):(?<id>\\d+)>)", RegexOptions.Compiled);

    private readonly IDiscordRestChannelAPI _channelApi;

    public EmojiIdCommand(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!emojiId", StringComparison.OrdinalIgnoreCase);

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        var matches = _emojiRegex.Matches(message.Content);

        StringBuilder sb = new();
        foreach (var matchGroups in matches.Select(match => match.Groups))
        {
            sb.Append(matchGroups["fullEmoji"]).Append(' ').Append(matchGroups["id"]);
        }

        await _channelApi.CreateMessageAsync(message.ChannelID, sb.ToString());

        return Result.FromSuccess();
    }
}