using FrogBot.ChatCommands;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChatCommand<TChatCommand>(this IServiceCollection @this)
        where TChatCommand : class, IChatCommand =>
            @this.AddScoped<IChatCommand, TChatCommand>();
    }
}