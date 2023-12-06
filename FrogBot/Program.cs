﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrogBot.ChatCommands;
using FrogBot.Responders;
using FrogBot.TikTok;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;

namespace FrogBot;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var services = host.Services;

        if (args.Any(arg => arg.Equals("--migrate")))
        {
            await using var db = services.GetRequiredService<VoteDbContext>();
            await db.Database.MigrateAsync();
            return;
        }

        var client = services.GetRequiredService<DiscordGatewayClient>();
        var cancellationSource = new CancellationTokenSource();
        await client.RunAsync(cancellationSource.Token);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(builder => builder.AddEnvironmentVariables())
            .ConfigureAppConfiguration((host, builder) => builder.AddCommandLine(args).AddConfigDirectory(host))
            .ConfigureServices(ConfigureServices);

    private static IConfiguration AddConfigDirectory(this IConfigurationBuilder @this, HostBuilderContext host) =>
        @this.AddJsonFile(Path.Combine("config", "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine("config", $"appsettings{host.HostingEnvironment.EnvironmentName}.json"), optional: true)
            .Build();

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddLogging(logging => logging.AddConsole().AddConfiguration(hostContext.Configuration.GetSection("Logging")));

        services.Configure<FrogBotOptions>(hostContext.Configuration.GetSection(ConfigurationKeys.FrogBot));
        services.Configure<VoteOptions>(hostContext.Configuration.GetSection(ConfigurationKeys.Voting));

        services.AddDbContext<VoteDbContext>(opt =>
            opt.UseNpgsql(hostContext.Configuration[ConfigurationKeys.ConnectionString]));

        services.AddTransient<IVoteManager, VoteManager>();
        services.AddTransient<IVoteEmojiProvider, VoteEmojiProvider>();
        services.AddTransient<IUsernameCachingService, UsernameCachingService>();
        services.AddTransient<IMessageRetriever, CachingMessageRetriever>();
        services.AddMemoryCache()
            .AddSingleton<BotMemoryCache>();

        services.Configure<TikTokOptions>(hostContext.Configuration.GetSection("TikTok"));
        services.AddTransient<ITikTokQuarantineResponder, TikTokChatResponder>();
        services.AddTransient<ITikTokQuarantineManager, TikTokQuarantineManager>();

        services.Configure<BotMemoryCacheOptions>(hostContext.Configuration.GetSection(ConfigurationKeys.Caching));

        services
            .AddDiscordGateway(sp => sp.GetRequiredService<IOptions<FrogBotOptions>>().Value.Token)
            .Configure<DiscordGatewayClientOptions>(opt =>
            {
                opt.Intents =
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.MessageContents |
                    GatewayIntents.GuildMessageReactions |
                    GatewayIntents.DirectMessages |
                    GatewayIntents.DirectMessageReactions;
            })
            .AddResponder<DelegatingChatResponder>()
            .AddResponder<MessageVoteCreationResponder>()
            .AddResponder<VoteAddResponder>()
            .AddResponder<VoteRemoveResponder>()
            .AddResponder<RemoveAllVotesResponder>()
            .AddResponder<DeleteMessageResponder>()
            .AddResponder<UsernameChangeResponder>()
            .AddResponder<MessageEditResponder>()
            .AddChatCommand<TestChatCommand>()
            .AddChatCommand<SayCommand>()
            .AddChatCommand<VersionCommand>()
            .AddChatCommand<TopChatCommand>()
            .AddChatCommand<WorstChatCommand>()
            .AddChatCommand<PointsChatCommand>()
            .AddChatCommand<EmojiIdCommand>()
            .AddChatCommand<VoteBanCommand>()
            .AddChatCommand<VoteUnbanCommand>()
            .AddChatResponder<ChatCommandResponder>()
            .AddChatResponder<TikTokChatResponder>();
    }
}
