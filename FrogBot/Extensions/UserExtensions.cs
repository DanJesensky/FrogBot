using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.Extensions;

public static class UserExtensions
{
    public static string GetFullUsername(this IUser @this) =>
        $"{@this.Username}#{@this.Discriminator}";
}