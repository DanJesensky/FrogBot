using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    public class EmojiIdCommand : IChatCommand
    {
        private static readonly Regex _emojiRegex = new Regex("(?<fullEmoji><:(?:[^:]+):(?<id>\\d+)>)", RegexOptions.Compiled);
        private readonly IDiscordRestChannelAPI _channelApi;
        
        public EmojiIdCommand(IDiscordRestChannelAPI channelApi)
        {
            _channelApi = channelApi;
        }

        public bool CanHandleCommand(IMessageCreate messageCreateEvent) =>
            messageCreateEvent.Content.StartsWith("!emojiId", StringComparison.OrdinalIgnoreCase);

        public async Task<Result> HandleCommandAsync(IMessageCreate messageCreateEvent)
        {
            var matches = _emojiRegex.Matches(messageCreateEvent.Content);

            StringBuilder sb = new();
            foreach (Match match in matches)
            {
                sb.Append(match.Groups["fullEmoji"]).Append(' ').Append(match.Groups["id"]);
            }

            await _channelApi.CreateMessageAsync(messageCreateEvent.ChannelID, sb.ToString());

            return Result.FromSuccess();
        }
    }
}