using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.ChatCommands.Authorization;

public class BotAdminAuthorizationAttribute : ChatCommandAuthorizationAttribute
{
    private const ulong Dan2997 = 159870805390524416;
    public override bool IsAuthorized(IUser user, IMessage message) =>
        user.ID.Value == Dan2997;
}