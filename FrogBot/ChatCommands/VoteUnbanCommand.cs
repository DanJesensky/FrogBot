using System;
using System.Linq;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands
{
    [BotAdminAuthorization]
    public class VoteUnbanCommand : IChatCommand
    {
        private readonly VoteDbContext _voteDbContext;
        private readonly IDiscordRestChannelAPI _channel;
        private readonly ILogger<VoteUnbanCommand> _logger;

        public VoteUnbanCommand(VoteDbContext voteDbContext, IDiscordRestChannelAPI channel, ILogger<VoteUnbanCommand> logger)
        {
            _voteDbContext = voteDbContext;
            _channel = channel;
            _logger = logger;
        }

        public bool CanHandleCommand(IMessage message) =>
            message.Content.StartsWith("!voteunban", StringComparison.OrdinalIgnoreCase);

        public async Task<Result> HandleCommandAsync(IMessage message)
        {
            if (message.Mentions.Count != 1)
            {
                await _channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");

                var mentions = string.Join(',', message.Mentions.Select(mention => $"{mention.Username}#{mention.Discriminator} ({mention.ID.Value})"));
                _logger.LogError("Failed to ban users from voting: multiple users were specified: [{mentions}]", mentions);
                return Result.FromSuccess();
            }

            var targetUser = message.Mentions[0];
            var bannedUser = new BannedVoter { UserId = targetUser.ID.Value };
            if (!await _voteDbContext.BannedVoters.ContainsAsync(bannedUser))
            {
                await _channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");
                _logger.LogError("Cannot ban {username}#{discriminator} ({id}) from voting: they are not banned.", targetUser.Username, targetUser.Discriminator, targetUser.ID.Value);
                return Result.FromSuccess();
            }

            try
            {
                _voteDbContext.BannedVoters.Remove(bannedUser);
                await _voteDbContext.SaveChangesAsync();
                _logger.LogInformation("{username}#{discriminator} ({id}) was unbanned from voting.", targetUser.Username, targetUser.Discriminator, targetUser.ID.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove banned user from database");
                await _channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");
            }

            return Result.FromSuccess();
        }
    }
}