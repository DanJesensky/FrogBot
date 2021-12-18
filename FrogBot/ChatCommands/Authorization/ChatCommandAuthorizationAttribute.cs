using System;
using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.ChatCommands.Authorization;

[AttributeUsage(AttributeTargets.Class)]
public abstract class ChatCommandAuthorizationAttribute : Attribute
{
    // Eventually this will probably become a ChatCommandContext object. As it stands, there is no way to get
    // data from the Discord API.
    public abstract bool IsAuthorized(IUser user, IMessage message);
}