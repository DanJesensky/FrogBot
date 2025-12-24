using System;
using System.Linq;
using System.Threading.Tasks;
using FrogBot.ChatCommands.Authorization;
using FrogBot.Voting;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;

namespace FrogBot.ChatCommands;

[BotAdminAuthorization]
[UsedImplicitly]
public class VoteBanCommand(VoteDbContext voteDbContext, IDiscordRestChannelAPI channel, ILogger<VoteBanCommand> logger)
    : IChatCommand
{
    public bool CanHandleCommand(IMessage message) =>
        message.Content.StartsWith("!voteban", StringComparison.OrdinalIgnoreCase);

    public async Task<Result> HandleCommandAsync(IMessage message)
    {
        if (message.Mentions.Count != 1)
        {
            await channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");

            var mentions = string.Join(',', message.Mentions.Select(mention => $"{mention.Username}#{mention.Discriminator} ({mention.ID.Value})"));
            logger.LogError("Failed to ban users from voting: multiple users were specified: [{mentions}]", mentions);
            return Result.FromSuccess();
        }

        var targetUser = message.Mentions[0];
        var bannedUser = new BannedVoter { UserId = targetUser.ID.Value };
        if (await voteDbContext.BannedVoters.ContainsAsync(bannedUser))
        {
            await channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");
            logger.LogError("Cannot ban {username}#{discriminator} ({id}) from voting: they are already banned.", targetUser.Username, targetUser.Discriminator, targetUser.ID.Value);
            return Result.FromSuccess();
        }

        try
        {
            voteDbContext.BannedVoters.Add(bannedUser);
            await voteDbContext.SaveChangesAsync();
            logger.LogInformation("{username}#{discriminator} ({id}) was banned from voting.", targetUser.Username, targetUser.Discriminator, targetUser.ID.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save banned user to database");
            await channel.CreateReactionAsync(message.ChannelID, message.ID, "❌");
        }

        return Result.FromSuccess();
    }
}