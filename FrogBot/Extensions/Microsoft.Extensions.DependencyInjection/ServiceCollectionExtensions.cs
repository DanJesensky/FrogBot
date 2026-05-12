using FrogBot.Responders;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatResponder<TChatResponder>(this IServiceCollection @this)
        where TChatResponder : class, IChatResponder =>
        @this.AddScoped<IChatResponder, TChatResponder>();
}
